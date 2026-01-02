using Management_System_WPF.Models;
using Management_System_WPF.Services;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Drawing;


namespace Management_System_WPF.Views
{
    public partial class ArticleReportPage : Page
    {
        private string _selectedArticle;
        private DateTime _selectedDate;


        private DateTime _currentMonth;
        // 🔥 NEW: Store article categories for lookup
        private Dictionary<string, string> _articleCategoryMap = new();

        public ArticleReportPage()
        {
            InitializeComponent();
            // 1. Load Categories and Build Map
            LoadCategoriesAndMap();

            ((MainWindow)Application.Current.MainWindow).ShowFullScreenPage();
            LoadSalesTillNow();


            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            txtTitle.Text = _currentMonth.ToString("MMMM yyyy");

            LoadArticleReport(_currentMonth);
        }
        // 🔥 NEW: Load Categories & Map Logic
        private void LoadCategoriesAndMap()
        {
            // A. Fetch all items to know which category they belong to
            var allItems = ItemsService.GetAllItems();

            // Build the dictionary: Key = Name, Value = Category
            _articleCategoryMap = allItems.ToDictionary(x => x.Name, x => x.Category);

            // B. Populate ComboBox (Same as InventoryPage)
            var categories = new List<string> { "All" }; // Default Option

            // Add distinct categories from DB or your hardcoded list
            categories.AddRange(allItems.Select(x => x.Category).Distinct().OrderBy(c => c));

            // Or use your hardcoded list if you prefer:
            // categories.AddRange(new[] { "Double Station", "Vertical", "Rotary" });

            cmbFilterCategory.ItemsSource = categories;
            cmbFilterCategory.SelectedIndex = 0; // Select "All" by default
        }
        // 🔥 NEW: Filter Change Event
        private void FilterCategory_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Reload the report. The LoadArticleReport function will check the combobox.
            LoadArticleReport(_currentMonth);
        }


        // LOAD ARTICLE REPORT FOR SPECIFIC MONTH

        private void LoadArticleReport(DateTime month)
        {
            var raw = SalesService.GetArticleSalesByMonth(month.Year, month.Month);
            // 2. 🔥 APPLY FILTER HERE 🔥
            if (cmbFilterCategory.SelectedItem != null)
            {
                string selectedFilter = cmbFilterCategory.SelectedItem.ToString();

                if (selectedFilter != "All")
                {
                    // Filter the raw list. 
                    // Keep the sale ONLY if its Article Name exists in our map AND matches the category.
                    raw = raw.Where(sale =>
                        _articleCategoryMap.ContainsKey(sale.Article) &&
                        _articleCategoryMap[sale.Article] == selectedFilter
                    ).ToList();
                }
            }

            var dates = raw.Select(r => r.Date).Distinct().ToList();
            var articles = raw.Select(r => r.Article).Distinct().OrderBy(a => a).ToList();

            List<ArticleSaleRow> rows = new();

            // 🔹 DAILY ROWS
            foreach (var dateStr in dates)
            {
                if (!DateTime.TryParse(dateStr, out DateTime parsedDate))
                    continue;

                var row = new ArticleSaleRow
                {
                    Date = parsedDate
                };

                foreach (var r in raw.Where(x => x.Date == dateStr))
                {
                    row.ArticleValues[r.Article] =
                      (row.ArticleValues.ContainsKey(r.Article)
                        ? row.ArticleValues[r.Article] ?? 0
                        : 0) + r.Qty;
                }

                rows.Add(row);
            }

            // 🔹 TOTAL ROW (ONLY ONCE)
            var totalRow = new ArticleSaleRow
            {
                IsTotalRow = true
            };

            foreach (var art in articles)
            {
                // Calculate Sum
                int sum = rows.Sum(r => r.ArticleValues.ContainsKey(art) ? r.ArticleValues[art] ?? 0 : 0);
                totalRow.ArticleValues[art] = sum;
            }
            // Calculate Grand Total for the "Total" column
           // totalRow.Total = totalRow.ArticleValues.Values.Sum(x => x ?? 0);
            rows.Add(totalRow);
            UpdateMonthNavigationButtons();

            BuildDynamicColumns(articles);
            dgArticles.ItemsSource = rows;
        }







        // CREATE DYNAMIC COLUMNS

        private void BuildDynamicColumns(List<string> articles)
        {
            dgArticles.Columns.Clear();

            // ✅ DATE COLUMN (ONLY ONCE)
            dgArticles.Columns.Add(new DataGridTextColumn
            {
                Header = "Date",
                Binding = new Binding("DateDisplay"), // 👈 IMPORTANT
                Width = 120,
                IsReadOnly = true
            });

            // ARTICLE COLUMNS
            foreach (var art in articles)
            {
                dgArticles.Columns.Add(new DataGridTextColumn
                {
                    Header = art,
                    Binding = new Binding($"ArticleValues[{art}]"),
                    Width = 100,
                    IsReadOnly = true
                });
            }

            // TOTAL COLUMN
            dgArticles.Columns.Add(new DataGridTextColumn
            {
                Header = "Total",
                Binding = new Binding("Total"),
                Width = 120,
                IsReadOnly = true
            });
        }





        // BUTTON: BACK

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Application.Current.MainWindow;
            main.ResetLayoutBeforeNavigation();
            main.ResizeMode = ResizeMode.CanResize;
            if (main.WindowState == WindowState.Maximized)
            {
                main.WindowState = WindowState.Normal;
                main.WindowState = WindowState.Maximized;
            }
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }


        // BUTTON: PREVIOUS MONTH

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var prev = SalesService.GetPreviousArticleSalesMonth(_currentMonth);

            if (prev != null)
            {
                _currentMonth = prev.Value;
                txtTitle.Text = _currentMonth.ToString("MMMM yyyy");
                LoadArticleReport(_currentMonth);
            }
        }



        // BUTTON: NEXT MONTH

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            var next = SalesService.GetNextArticleSalesMonth(_currentMonth);

            if (next != null)
            {
                _currentMonth = next.Value;
                txtTitle.Text = _currentMonth.ToString("MMMM yyyy");
                LoadArticleReport(_currentMonth);
            }
        }


        // EXPORT EXCEL

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validate Data
            var rows = dgArticles.ItemsSource as List<ArticleSaleRow>;
            if (rows == null || rows.Count == 0)
            {
                MessageBox.Show("No data available to export.");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"ArticleReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Sales Report");

                    // --- 1. PREPARE HEADERS ---

                    // Get all unique article names
                    var allArticleNames = rows.SelectMany(r => r.ArticleValues.Keys)
                                              .Distinct()
                                              .OrderBy(x => x)
                                              .ToList();

                    // Dictionary to hold the vertical sums (Bottom Row)
                    var columnTotals = allArticleNames.ToDictionary(name => name, name => 0m);

                    ws.Cells[1, 1].Value = "Date";

                    int col = 2;
                    foreach (var artName in allArticleNames)
                    {
                        ws.Cells[1, col++].Value = artName;
                    }

                    // [NEW] Add "Total" Header at the end
                    ws.Cells[1, col].Value = "Total";
                    ws.Cells[1, col].Style.Font.Bold = true;


                    // --- 2. WRITE DATA ROWS ---
                    int rowIdx = 2;
                    decimal grandTotal = 0; // Bottom-right corner value

                    foreach (var r in rows)
                    {
                        if (r.Date.Year < 1900) continue;
                        // Date Column
                        ws.Cells[rowIdx, 1].Value = r.Date;
                        ws.Cells[rowIdx, 1].Style.Numberformat.Format = "dd-mm-yyyy";

                        col = 2;
                        decimal rowSum = 0; // Horizontal sum for this specific day

                        foreach (var artName in allArticleNames)
                        {
                            decimal val = 0;
                            if (r.ArticleValues.ContainsKey(artName) && r.ArticleValues[artName].HasValue)
                            {
                                val = Convert.ToDecimal(r.ArticleValues[artName].Value);
                            }

                            // Write Value
                            if (val > 0) ws.Cells[rowIdx, col].Value = val;
                            else ws.Cells[rowIdx, col].Value = 0;

                            // [CALC] Add to Row Sum (Horizontal)
                            rowSum += val;

                            // [CALC] Add to Column Sum (Vertical)
                            columnTotals[artName] += val;

                            col++;
                        }

                        // [NEW] Write Row Total (Last Column)
                        ws.Cells[rowIdx, col].Value = rowSum;
                        ws.Cells[rowIdx, col].Style.Font.Bold = true;
                        ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(Color.WhiteSmoke);

                        grandTotal += rowSum; // Add to grand total
                        rowIdx++;
                    }


                    // --- 3. WRITE BOTTOM TOTAL ROW ---

                    // Write "Total" label
                    ws.Cells[rowIdx, 1].Value = "Total";
                    ws.Cells[rowIdx, 1].Style.Font.Bold = true;
                    ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    col = 2;
                    foreach (var artName in allArticleNames)
                    {
                        // Write the vertical sum we calculated earlier
                        decimal colSum = columnTotals[artName];
                        ws.Cells[rowIdx, col].Value = colSum;
                        col++;
                    }

                    // [NEW] Write Grand Total (Bottom Right)
                    ws.Cells[rowIdx, col].Value = grandTotal;
                    ws.Cells[rowIdx, col].Style.Font.Bold = true;
                    ws.Cells[rowIdx, col].Style.Font.Size = 12;
                    ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);


                    // --- 4. STYLING ---

                    // Format the Total Row (Bottom)
                    var bottomRowRange = ws.Cells[rowIdx, 1, rowIdx, col];
                    bottomRowRange.Style.Font.Bold = true;
                    bottomRowRange.Style.Border.Top.Style = ExcelBorderStyle.Medium; // Thicker line above totals

                    // Header Style
                    var headerRange = ws.Cells[1, 1, 1, col];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.Teal);
                    headerRange.Style.Font.Color.SetColor(Color.White);

                    // Borders for all data
                    var fullRange = ws.Cells[1, 1, rowIdx, col];
                    fullRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    fullRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    fullRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    ws.View.FreezePanes(2, 1);
                    ws.Cells.AutoFitColumns();

                    // --- SAVE ---
                    File.WriteAllBytes(saveFileDialog.FileName, package.GetAsByteArray());

                    MessageBox.Show("Export Successful!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }




        // DISABLE NEXT/PREV IF MONTH HAS NO SALES

        private void UpdateMonthNavigationButtons()
        {
            btnPrevMonth.IsEnabled =
              SalesService.GetPreviousArticleSalesMonth(_currentMonth) != null;

            btnNextMonth.IsEnabled =
              SalesService.GetNextArticleSalesMonth(_currentMonth) != null;

            btnPrevMonth.Opacity = btnPrevMonth.IsEnabled ? 1.0 : 0.4;
            btnNextMonth.Opacity = btnNextMonth.IsEnabled ? 1.0 : 0.4;
        }

        private void dgArticles_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dgArticles.SelectedCells.Count == 0)
                return;

            var cell = dgArticles.SelectedCells[0];
            var row = cell.Item as ArticleSaleRow;

            // Ignore Date column
            if (row == null || cell.Column.DisplayIndex == 0)
                return;

            _selectedDate = row.Date;
            _selectedArticle = cell.Column.Header.ToString();
        }



       

        private void LoadSalesTillNow()
        {
            var raw = SalesService.GetArticleSalesTillNow();

            var articles = raw.Select(x => x.Article).Distinct().ToList();

            dgSalesTillNow.Columns.Clear();

            // First column
            dgSalesTillNow.Columns.Add(new DataGridTextColumn
            {
                Header = "Articles →",
                Binding = new Binding("Key"),
                Width = 160
            });

            // Total row data
            Dictionary<string, int> totals = new();

            foreach (var art in articles)
            {
                int sum = raw.Where(x => x.Article == art).Sum(x => x.Qty);
                totals[art] = sum;

                dgSalesTillNow.Columns.Add(new DataGridTextColumn
                {
                    Header = art,
                    Binding = new Binding($"Value[{art}]"),
                    Width = 100
                });
            }

            dgSalesTillNow.ItemsSource = new[]
            {
    new
    {
      Key = "Total →",
      Value = totals
    }
  };
        }


    }
}
