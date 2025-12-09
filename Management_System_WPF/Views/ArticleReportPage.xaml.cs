using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using PdfSharp.Pdf;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;


namespace Management_System_WPF.Views
{
    public partial class ArticleReportPage : Page
    {
        public ArticleReportPage()
        {
            InitializeComponent();
            txtTitle.Text = DateTime.Now.ToString("MMMM yyyy");
            LoadArticleReport();
        }

        private void LoadArticleReport()
        {
            var raw = SalesService.GetArticleSales();

            var dates = raw.Select(r => r.Date).Distinct().ToList();
            var articles = raw.Select(r => r.Article).Distinct().ToList();

            List<ArticleSaleRow> rows = new();

            foreach (var date in dates)
            {
                var row = new ArticleSaleRow();
                row.Date = date;

                // initialize values
                foreach (var art in articles)
                    row.ArticleValues[art] = 0;

                // fill values
                foreach (var r in raw.Where(x => x.Date == date))
                {
                    row.ArticleValues[r.Article] += r.Total;
                }

                rows.Add(row);
            }

            BuildDynamicColumns(articles);
            dgArticles.ItemsSource = rows;
        }

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

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var raw = SalesService.GetArticleSalesPreviousMonth();

            var dates = raw.Select(r => r.Date).Distinct().ToList();
            var articles = raw.Select(r => r.Article).Distinct().ToList();

            List<ArticleSaleRow> rows = new();

            foreach (var date in dates)
            {
                ArticleSaleRow row = new();
                row.Date = date;

                foreach (var a in articles)
                    row.ArticleValues[a] = 0;

                foreach (var r in raw.Where(x => x.Date == date))
                {
                    row.ArticleValues[r.Article] += r.Total;
                }

                rows.Add(row);
            }

            BuildDynamicColumns(articles);
            dgArticles.ItemsSource = rows;

            MessageBox.Show("Loaded Previous Month Records");
        }


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
                {
                    ws.Cells[1, col++].Value = a;
                }

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
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                dgArticles.Margin = new Thickness(20);
                printDlg.PrintVisual(dgArticles, "Sale By Article Report");
            }
        }




    }
}
