using Management_System_WPF.Models;
using Management_System_WPF.Services;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Management_System_WPF.Views
{
    public partial class ArticleReportPage : Page
    {
        private string _selectedArticle;
        private DateTime _selectedDate;


        private DateTime _currentMonth;

        public ArticleReportPage()
        {
            InitializeComponent();

            ((MainWindow)Application.Current.MainWindow).ShowFullScreenPage();
            LoadSalesTillNow();


            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            txtTitle.Text = _currentMonth.ToString("MMMM yyyy");

            LoadArticleReport(_currentMonth);
        }


        // LOAD ARTICLE REPORT FOR SPECIFIC MONTH

        private void LoadArticleReport(DateTime month)
        {
            var raw = SalesService.GetArticleSalesByMonth(month.Year, month.Month);

            var dates = raw.Select(r => r.Date).Distinct().ToList();
            var articles = raw.Select(r => r.Article).Distinct().ToList();

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
                totalRow.ArticleValues[art] =
                    rows.Sum(r => r.ArticleValues.ContainsKey(art)
                        ? r.ArticleValues[art] ?? 0
                        : 0);
            }

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
            var rows = dgArticles.ItemsSource as List<ArticleSaleRow>;
            if (rows == null || rows.Count == 0)
            {
                MessageBox.Show("No data to export!");
                return;
            }

            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Report");

                ws.Cells[1, 1].Value = "Date";
                int col = 2;

                var articleNames = rows.First().ArticleValues.Keys.ToList();
                foreach (var a in articleNames)
                    ws.Cells[1, col++].Value = a;

                int rowIndex = 2;
                foreach (var r in rows)
                {
                    ws.Cells[rowIndex, 1].Value = r.Date;
                    ws.Cells[rowIndex, 1].Style.Numberformat.Format = "dd/mm/yyyy";


                    col = 2;
                    foreach (var art in articleNames)
                    {
                        if (r.ArticleValues.ContainsKey(art) && r.ArticleValues[art].HasValue)
                            ws.Cells[rowIndex, col++].Value = r.ArticleValues[art].Value;
                        else
                            ws.Cells[rowIndex, col++].Value = ""; // empty cell
                    }


                    rowIndex++;
                }

                string filePath = $"ArticleReport_{DateTime.Now:yyyyMMdd}.xlsx";
                File.WriteAllBytes(filePath, package.GetAsByteArray());

                MessageBox.Show($"Excel Exported Successfully!\n{filePath}");
            }
        }


        // PRINT REPORT

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                dgArticles.Margin = new Thickness(20);
                pd.PrintVisual(dgArticles, "Sale By Article Report");
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



        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedArticle))
            {
                MessageBox.Show("Please select an article cell to edit.");
                return;
            }

            EditArticleSaleWindow win =
                new EditArticleSaleWindow(_selectedArticle, _selectedDate);

            if (win.ShowDialog() == true)
            {
                LoadArticleReport(_currentMonth);
            }
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
