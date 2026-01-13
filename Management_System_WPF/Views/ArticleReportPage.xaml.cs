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
            cmbSaleTillNowFilterCategory.ItemsSource = cmbFilterCategory.ItemsSource;
            cmbSaleTillNowFilterCategory.SelectedIndex = 0;



            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            txtTitle.Text = _currentMonth.ToString("MMMM yyyy");
            LoadYearList();

            LoadArticleReport(_currentMonth);
        }
        private void LoadYearList()
        {
            var raw = SalesService.GetArticleSalesTillNowRaw();

            if (raw == null) return;

            // Extract available years using the sale row dates
            var years = raw
                .Select(r =>
                {
                    if (DateTime.TryParse(r.Date, out DateTime dt))
                        return dt.Year;
                    return (int?)null;
                })
                .Where(y => y.HasValue)
                .Select(y => y.Value)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            cmbYearFilter.Items.Clear();
            cmbYearFilter.Items.Add("All");

            foreach (var y in years)
                cmbYearFilter.Items.Add(y.ToString());

            cmbYearFilter.SelectedIndex = 0;
        }

        private void LoadArticleReportByRange(DateTime from, DateTime to)
        {
            // 🔹 Fetch raw data for range
            var raw = SalesService.GetArticleSalesByDateRange(from, to);

            // 🔹 Apply category filter (reuse existing logic)
            if (cmbFilterCategory.SelectedItem != null &&
                cmbFilterCategory.SelectedItem.ToString() != "All")
            {
                string selectedFilter = cmbFilterCategory.SelectedItem.ToString();

                raw = raw.Where(s =>
                    _articleCategoryMap.ContainsKey(s.Article) &&
                    _articleCategoryMap[s.Article] == selectedFilter
                ).ToList();
            }

            var dates = raw.Select(r => r.Date).Distinct().ToList();
            var articles = raw.Select(r => r.Article).Distinct().OrderBy(a => a).ToList();

            List<ArticleSaleRow> rows = new();

            foreach (var dateStr in dates)
            {
                if (!DateTime.TryParse(dateStr, out DateTime parsedDate))
                    continue;

                var row = new ArticleSaleRow
                {
                    Date = parsedDate
                };

                foreach (var art in articles)
                {
                    row.ArticleValues[art] = string.Empty;
                    row.ArticleTooltips[art] = string.Empty;

                    var entries = raw
                        .Where(r => r.Date == dateStr && r.Article == art)
                        .Select(r => new { r.Qty, r.BuyerName })
                        .ToList();

                    if (entries.Any())
                    {
                        int qtySum = entries.Sum(e => e.Qty);

                        // Show only SUM inside the cell
                        row.ArticleValues[art] = qtySum.ToString();

                        // Tooltip still shows individual breakdown
                        row.ArticleTooltips[art] = string.Join(
                            Environment.NewLine,
                            entries.Select(e => $"{e.Qty} → {e.BuyerName}")
                        );
                    }

                }

                rows.Add(row);
            }

            // 🔹 TOTAL ROW
            var totalRow = new ArticleSaleRow { IsTotalRow = true };

            foreach (var art in articles)
            {
                totalRow.ArticleValues[art] =
                    raw.Where(r => r.Article == art).Sum(r => r.Qty).ToString();
            }

            rows.Add(totalRow);

            // 🔹 Bind
            BuildDynamicColumns(articles);
            dgArticles.ItemsSource = rows;

            // 🔹 Disable month navigation (range mode)
            btnPrevMonth.IsEnabled = false;
            btnNextMonth.IsEnabled = false;
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

        private void FilterCategory_ChangedSaleTillNow(object sender, SelectionChangedEventArgs e)
        {
           
            LoadSalesTillNow();
        }
        // LOAD ARTICLE REPORT FOR SPECIFIC MONTH

        private void LoadArticleReport(DateTime month)
        {
            var raw = SalesService.GetArticleSalesByMonth(month.Year, month.Month);

            // Category filter
            if (cmbFilterCategory.SelectedItem != null &&
                cmbFilterCategory.SelectedItem.ToString() != "All")
            {
                string selectedFilter = cmbFilterCategory.SelectedItem.ToString();

                raw = raw.Where(s =>
                    _articleCategoryMap.ContainsKey(s.Article) &&
                    _articleCategoryMap[s.Article] == selectedFilter
                ).ToList();
            }

            var dates = raw.Select(r => r.Date).Distinct().ToList();
            var articles = raw.Select(r => r.Article).Distinct().OrderBy(a => a).ToList();

            List<ArticleSaleRow> rows = new();

            foreach (var dateStr in dates)
            {
                if (!DateTime.TryParse(dateStr, out DateTime parsedDate))
                    continue;

                var row = new ArticleSaleRow
                {
                    Date = parsedDate
                };

                foreach (var art in articles)
                {
                    row.ArticleValues[art] = string.Empty;
                    row.ArticleTooltips[art] = string.Empty;

                    var entries = raw
                        .Where(r => r.Date == dateStr && r.Article == art)
                        .Select(r => new { r.Qty, r.BuyerName })
                        .ToList();

                    if (entries.Any())
                    {
                        int qtySum = entries.Sum(e => e.Qty);

                        // Show only SUM inside the cell
                        row.ArticleValues[art] = qtySum.ToString();

                        // Tooltip still shows individual breakdown
                        row.ArticleTooltips[art] = string.Join(
                            Environment.NewLine,
                            entries.Select(e => $"{e.Qty} → {e.BuyerName}")
                        );
                    }

                }

                rows.Add(row);
            }

            // TOTAL ROW
            var totalRow = new ArticleSaleRow { IsTotalRow = true };

            foreach (var art in articles)
            {
                totalRow.ArticleValues[art] =
                    raw.Where(r => r.Article == art).Sum(r => r.Qty).ToString();
            }

            rows.Add(totalRow);

            BuildDynamicColumns(articles);
            dgArticles.ItemsSource = rows;
            UpdateMonthNavigationButtons();
        }








        // CREATE DYNAMIC COLUMNS

        private void BuildDynamicColumns(List<string> articles)
        {
            dgArticles.Columns.Clear();

            // DATE COLUMN
            dgArticles.Columns.Add(new DataGridTextColumn
            {
                Header = "Date",
                Binding = new Binding("DateDisplay"),
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
                    IsReadOnly = true,
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters =
    {
        new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap),
        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center),
        new Setter(TextBlock.ToolTipProperty, null) ,// default: NO tooltip
        // Tooltip binding when content exists
new Setter(
    TextBlock.ToolTipProperty,
    new Binding($"ArticleTooltips[{art}]")
)

    },
                        Triggers =
    {
        // ✅ SHOW tooltip only when value is NOT empty
        new DataTrigger
        {
            Binding = new Binding($"ArticleValues[{art}]"),
            Value = "",
            Setters =
            {
                new Setter(TextBlock.ToolTipProperty, null)
            }
        },
        new DataTrigger
        {
            Binding = new Binding($"ArticleValues[{art}]"),
            Value = "{x:Null}",
            Setters =
            {
                new Setter(TextBlock.ToolTipProperty, null)
            }
        },
        new DataTrigger
        {
            Binding = new Binding($"ArticleValues[{art}]"),
            Value = "",
            Setters = { }
        }
    }
                    }

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

                            if (r.ArticleValues.ContainsKey(artName) &&
                                !string.IsNullOrWhiteSpace(r.ArticleValues[artName]))
                            {
                                val = r.ArticleValues[artName]
                                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(x => decimal.Parse(x))
                                    .Sum();
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

            var cellInfo = dgArticles.SelectedCells[0];
            var row = cellInfo.Item as ArticleSaleRow;

            // ❌ Ignore invalid clicks
            if (row == null || row.IsTotalRow || cellInfo.Column.DisplayIndex == 0)
                return;

            string article = cellInfo.Column.Header.ToString();

            // ❌ No data → no popup
            if (!row.ArticleTooltips.ContainsKey(article) ||
                string.IsNullOrWhiteSpace(row.ArticleTooltips[article]))
                return;

            // Parse tooltip data
            var details = row.ArticleTooltips[article]
                .Split('\n')
                .Select(line =>
                {
                    var parts = line.Split("→");
                    return (
                        Qty: int.Parse(parts[0].Trim()),
                        Buyer: parts[1].Trim()
                    );
                })
                .ToList();

            // Open popup
            var popup = new BuyerDetailsWindow(
                article,
                row.DateDisplay,
                details
            )
            {
                Owner = Window.GetWindow(this)
            };

            popup.ShowDialog();
        }






        private void LoadSalesTillNow(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                // 1️⃣ Fetch raw rows (each sale with real date)
                var raw = SalesService.GetArticleSalesTillNowRaw();

                if (raw == null) return;

                // 2️⃣ Category Filter
                if (cmbSaleTillNowFilterCategory.SelectedItem != null &&
                    cmbSaleTillNowFilterCategory.SelectedItem.ToString() != "All")
                {
                    string selectedCategory = cmbSaleTillNowFilterCategory.SelectedItem.ToString();

                    raw = raw.Where(x =>
                        _articleCategoryMap.ContainsKey(x.Article) &&
                        _articleCategoryMap[x.Article] == selectedCategory
                    ).ToList();
                }

                // 3️⃣ Date Range Filter
                if (from.HasValue && to.HasValue)
                {
                    raw = raw.Where(x =>
                    {
                        string[] formats = { "yyyy-MM-dd" };

                        if (DateTime.TryParseExact(x.Date,
                                                   formats,
                                                   CultureInfo.InvariantCulture,
                                                   DateTimeStyles.None,
                                                   out DateTime dt))
                        {
                            return dt.Date >= from.Value.Date && dt.Date <= to.Value.Date;
                        }
                        return false;
                    }).ToList();
                }

                // 4️⃣ Group AFTER FILTERING
                var articles = raw.Select(x => x.Article).Distinct().OrderBy(a => a).ToList();

                var totals = articles.ToDictionary(
                    art => art,
                    art => raw.Where(x => x.Article == art).Sum(x => x.Qty)
                );

                // 5️⃣ Build dynamic columns
                dgSalesTillNow.Columns.Clear();

                dgSalesTillNow.Columns.Add(new DataGridTextColumn
                {
                    Header = "Articles →",
                    Binding = new Binding("Key"),
                    Width = 150
                });

                foreach (var art in articles)
                {
                    dgSalesTillNow.Columns.Add(new DataGridTextColumn
                    {
                        Header = art,
                        Binding = new Binding($"Value[{art}]"),
                        Width = 100
                    });
                }

                // 6️⃣ Bind final grouped totals
                dgSalesTillNow.ItemsSource = new[]
                {
            new { Key = "Total →", Value = totals }
        };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void cmbYearFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbYearFilter.SelectedItem == null) return;

            string selected = cmbYearFilter.SelectedItem.ToString();

            if (selected == "All")
            {
                LoadSalesTillNow();
                return;
            }

            int year = int.Parse(selected);

            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31);

            LoadSalesTillNow(start, end);
        }

       

       

        private void MonthFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMonthFilter.SelectedItem == null) return;

            string selected = ((ComboBoxItem)cmbMonthFilter.SelectedItem).Content.ToString();

            if (selected == "All")
            {
                LoadSalesTillNow();
                return;
            }

            int monthNumber = DateTime.ParseExact(selected, "MMMM", CultureInfo.InvariantCulture).Month;

            var start = new DateTime(DateTime.Today.Year, monthNumber, 1);
            var end = start.AddMonths(1).AddDays(-1);

            LoadSalesTillNow(start, end);
        }


        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility =
                FilterPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        private void FilterThisMonth_Click(object sender, RoutedEventArgs e)
        {
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            LoadArticleReportByRange(start, end);
            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterSixMonths_Click(object sender, RoutedEventArgs e)
        {
            var end = DateTime.Today;
            var start = end.AddMonths(-6);

            LoadArticleReportByRange(start, end);
            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterYear_Click(object sender, RoutedEventArgs e)
        {
            var start = new DateTime(DateTime.Today.Year, 1, 1);
            var end = new DateTime(DateTime.Today.Year, 12, 31);

            LoadArticleReportByRange(start, end);
            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterCustom_Click(object sender, RoutedEventArgs e)
        {
            if (dpFrom.SelectedDate == null || dpTo.SelectedDate == null)
            {
                MessageBox.Show("Please select both From and To dates",
                    "Invalid Filter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpFrom.SelectedDate > dpTo.SelectedDate)
            {
                MessageBox.Show("From date cannot be greater than To date",
                    "Invalid Filter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadArticleReportByRange(dpFrom.SelectedDate.Value, dpTo.SelectedDate.Value);
            FilterPanel.Visibility = Visibility.Collapsed;
        }

        private void dgArticles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
