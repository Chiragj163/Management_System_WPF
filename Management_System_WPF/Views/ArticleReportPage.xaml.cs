using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OfficeOpenXml;
using System.IO;

namespace Management_System_WPF.Views
{
    public partial class ArticleReportPage : Page
    {
        // ✅ Declare here (class level)
        private DateTime _currentMonth;

        public ArticleReportPage()
        {
            InitializeComponent();

            ((MainWindow)Application.Current.MainWindow).ShowFullScreenPage();

            // Start at current month (first day)
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            txtTitle.Text = _currentMonth.ToString("MMMM yyyy");

            LoadArticleReport(_currentMonth);
        }

        // ========================================================
        // LOAD ARTICLE REPORT FOR SPECIFIC MONTH
        // ========================================================
        private void LoadArticleReport(DateTime month)
        {
            var raw = SalesService.GetArticleSalesByMonth(month.Year, month.Month);

            var dates = raw.Select(r => r.Date).Distinct().ToList();
            var articles = raw.Select(r => r.Article).Distinct().ToList();

            List<ArticleSaleRow> rows = new();

            foreach (var date in dates)
            {
                var row = new ArticleSaleRow();
                row.Date = date;

                foreach (var art in articles)
                    row.ArticleValues[art] = 0;

                foreach (var r in raw.Where(x => x.Date == date))
                    row.ArticleValues[r.Article] += r.Qty;

                rows.Add(row);
            }

            BuildDynamicColumns(articles);
            dgArticles.ItemsSource = rows;

            // Update next/prev button states
            UpdateMonthNavigationButtons();
        }

        // ========================================================
        // CREATE DYNAMIC COLUMNS
        // ========================================================
        private void BuildDynamicColumns(List<string> articles)
        {
            dgArticles.Columns.Clear();

            dgArticles.Columns.Add(new DataGridTextColumn
            {
                Header = "Date",
                Binding = new Binding("Date"),
                Width = 120
            });

            foreach (var art in articles)
            {
                dgArticles.Columns.Add(new DataGridTextColumn
                {
                    Header = art,
                    Binding = new Binding($"ArticleValues[{art}]"),
                    Width = 100
                });
            }
        }

        // ========================================================
        // BUTTON: BACK
        // ========================================================
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Application.Current.MainWindow;
            main.ResetLayoutBeforeNavigation();
            NavigationService.GoBack();
        }

        // ========================================================
        // BUTTON: PREVIOUS MONTH
        // ========================================================
        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);

            txtTitle.Text = _currentMonth.ToString("MMMM yyyy");
            LoadArticleReport(_currentMonth);
        }

        // ========================================================
        // BUTTON: NEXT MONTH
        // ========================================================
        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);

            txtTitle.Text = _currentMonth.ToString("MMMM yyyy");
            LoadArticleReport(_currentMonth);
        }

        // ========================================================
        // EXPORT EXCEL
        // ========================================================
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

                    col = 2;
                    foreach (var val in r.ArticleValues.Values)
                        ws.Cells[rowIndex, col++].Value = val;

                    rowIndex++;
                }

                string filePath = $"ArticleReport_{DateTime.Now:yyyyMMdd}.xlsx";
                File.WriteAllBytes(filePath, package.GetAsByteArray());

                MessageBox.Show($"Excel Exported Successfully!\n{filePath}");
            }
        }

        // ========================================================
        // PRINT REPORT
        // ========================================================
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                dgArticles.Margin = new Thickness(20);
                pd.PrintVisual(dgArticles, "Sale By Article Report");
            }
        }

        // ========================================================
        // DISABLE NEXT/PREV IF MONTH HAS NO SALES
        // ========================================================
        private void UpdateMonthNavigationButtons()
        {
            DateTime prev = _currentMonth.AddMonths(-1);
            DateTime next = _currentMonth.AddMonths(1);

            btnPrevMonth.IsEnabled = SalesService.HasSalesInMonth(prev.Year, prev.Month);
            btnNextMonth.IsEnabled = SalesService.HasSalesInMonth(next.Year, next.Month);

            btnPrevMonth.Opacity = btnPrevMonth.IsEnabled ? 1.0 : 0.4;
            btnNextMonth.Opacity = btnNextMonth.IsEnabled ? 1.0 : 0.4;
        }

    }
}
