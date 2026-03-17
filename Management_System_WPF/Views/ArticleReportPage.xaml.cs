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
using System.Windows.Input;
using System.Windows.Media;



namespace Management_System_WPF.Views
{
    public partial class ArticleReportPage : Page
    {
        private string _selectedArticle;
        private DateTime _selectedDate;

        private bool _isRangeMode = false;
        private DateTime _rangeFrom;
        private DateTime _rangeTo;

        private DateTime _currentMonth;
        private Dictionary<string, string> _articleCategoryMap = new();

        public ArticleReportPage()
        {
            InitializeComponent();
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
            _isRangeMode = true;
            _rangeFrom = from;
            _rangeTo = to;
            if (from.Month == to.Month && from.Year == to.Year)
            {
               
                txtTitle.Text = from.ToString("MMMM yyyy");
            }
            else
            {
                txtTitle.Text = $"{from:dd-MMM-yy} to {to:dd-MMM-yy}";
            }
            var raw = SalesService.GetArticleSalesByDateRange(from, to);

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

            var articleTotals = raw
                .GroupBy(r => r.Article)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var articles = articleTotals
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

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

                      
                        row.ArticleValues[art] = qtySum.ToString();

                      
                        row.ArticleTooltips[art] = string.Join(
                            Environment.NewLine,
                            entries.Select(e => $"{e.Qty} → {e.BuyerName}")
                        );
                    }

                }

                rows.Add(row);
            }

         
            var totalRow = new ArticleSaleRow { IsTotalRow = true };

            foreach (var art in articles)
            {
                totalRow.ArticleValues[art] =
                    raw.Where(r => r.Article == art).Sum(r => r.Qty).ToString();
            }

            rows.Add(totalRow);

           
            BuildDynamicColumns(articles);
            dgArticles.ItemsSource = rows;

           
            btnPrevMonth.IsEnabled = false;
            btnNextMonth.IsEnabled = false;
        }

       
        private void LoadCategoriesAndMap()
        {
            var allItems = ItemsService.GetAllItems();

            _articleCategoryMap = allItems.ToDictionary(x => x.Name, x => x.Category);

            var categories = new List<string> { "All" }; 
            categories.AddRange(allItems.Select(x => x.Category).Distinct().OrderBy(c => c));


            cmbFilterCategory.ItemsSource = categories;
            cmbFilterCategory.SelectedIndex = 0;
        }
        private void FilterCategory_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isRangeMode)
                LoadArticleReportByRange(_rangeFrom, _rangeTo);
            else
                LoadArticleReport(_currentMonth);

        }

      
     

        private void LoadArticleReport(DateTime month)
        {
            _isRangeMode = false;
            var raw = SalesService.GetArticleSalesByMonth(month.Year, month.Month);

            if (cmbFilterCategory.SelectedItem != null &&
                cmbFilterCategory.SelectedItem.ToString() != "All")
            {
                string selectedFilter = cmbFilterCategory.SelectedItem.ToString();

                raw = raw.Where(s =>
                    _articleCategoryMap.ContainsKey(s.Article) &&
                    _articleCategoryMap[s.Article] == selectedFilter
                ).ToList();
            }

            var dates = raw.Select(r => DateTime.Parse(r.Date)).Distinct().OrderBy(d => d).ToList();

            var articleTotals = raw
                .GroupBy(r => r.Article)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var articles = articleTotals
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            List<ArticleSaleRow> rows = new();

            foreach (var date in dates)
            {
                var row = new ArticleSaleRow
                {
                    Date = date
                };

                foreach (var art in articles)
                {
                    row.ArticleValues[art] = string.Empty;
                    row.ArticleTooltips[art] = string.Empty;

                    var entries = raw
     .Where(r =>
         DateTime.TryParse(r.Date, out var d) &&
         d.Date == date.Date &&
         r.Article == art
     )
     .Select(r => new { r.Qty, r.BuyerName })
     .ToList();


                    if (entries.Any())
                    {
                        int qtySum = entries.Sum(e => e.Qty);

                        row.ArticleValues[art] = qtySum.ToString();

                        row.ArticleTooltips[art] = string.Join(
                            Environment.NewLine,
                            entries.Select(e => $"{e.Qty} → {e.BuyerName}")
                        );
                    }

                }

                rows.Add(row);
            }

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




        private void BuildDynamicColumns(List<string> articles)
        {
            dgArticles.Columns.Clear();

            dgArticles.Columns.Add(new DataGridTextColumn
            {
                Header = "Date",
                Binding = new Binding("DateDisplay"),
                Width = 120,
                IsReadOnly = true
            });
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
        new Setter(TextBlock.ToolTipProperty, null) ,
new Setter(
    TextBlock.ToolTipProperty,
    new Binding($"ArticleTooltips[{art}]")
)

    },
                        Triggers =
    {
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

            dgArticles.Columns.Add(new DataGridTextColumn
            {
                Header = "Total",
                Binding = new Binding("Total"),
                Width = 120,
                IsReadOnly = true,

                 CellStyle = new Style(typeof(DataGridCell))
                 {
                     Setters =
        {
            new Setter(DataGridCell.BackgroundProperty, Brushes.LightGreen),
            new Setter(DataGridCell.FontWeightProperty, FontWeights.Bold)
        }
                 },

                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
        {
            new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center)
        }
                }
            });
        }




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

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            
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

                    var allArticleNames = rows.SelectMany(r => r.ArticleValues.Keys)
                                              .Distinct()
                                              .OrderBy(x => x)
                                              .ToList();

                    var columnTotals = allArticleNames.ToDictionary(name => name, name => 0m);

                    ws.Cells[1, 1].Value = "Date";

                    int col = 2;
                    foreach (var artName in allArticleNames)
                    {
                        ws.Cells[1, col++].Value = artName;
                    }
                    ws.Cells[1, col].Value = "Total";
                    ws.Cells[1, col].Style.Font.Bold = true;
                    int rowIdx = 2;
                    decimal grandTotal = 0; 

                    foreach (var r in rows)
                    {
                        if (r.Date.Year < 1900) continue;
                        ws.Cells[rowIdx, 1].Value = r.Date;
                        ws.Cells[rowIdx, 1].Style.Numberformat.Format = "dd-mm-yyyy";

                        col = 2;
                        decimal rowSum = 0; 

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

                            if (val > 0) ws.Cells[rowIdx, col].Value = val;
                            else ws.Cells[rowIdx, col].Value = 0;

                            rowSum += val;
                            columnTotals[artName] += val;

                            col++;
                        }
                        ws.Cells[rowIdx, col].Value = rowSum;
                        ws.Cells[rowIdx, col].Style.Font.Bold = true;
                        ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.WhiteSmoke);

                        grandTotal += rowSum; 
                        rowIdx++;
                    }

                    ws.Cells[rowIdx, 1].Value = "Total";
                    ws.Cells[rowIdx, 1].Style.Font.Bold = true;
                    ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    col = 2;
                    foreach (var artName in allArticleNames)
                    {
                        decimal colSum = columnTotals[artName];
                        ws.Cells[rowIdx, col].Value = colSum;
                        col++;
                    }

                    ws.Cells[rowIdx, col].Value = grandTotal;
                    ws.Cells[rowIdx, col].Style.Font.Bold = true;
                    ws.Cells[rowIdx, col].Style.Font.Size = 12;
                    ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    var bottomRowRange = ws.Cells[rowIdx, 1, rowIdx, col];
                    bottomRowRange.Style.Font.Bold = true;
                    bottomRowRange.Style.Border.Top.Style = ExcelBorderStyle.Medium; 

                    var headerRange = ws.Cells[1, 1, 1, col];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Teal);
                    headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);

                    var fullRange = ws.Cells[1, 1, rowIdx, col];
                    fullRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    fullRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    fullRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    ws.View.FreezePanes(2, 1);
                    ws.Cells.AutoFitColumns();

                    File.WriteAllBytes(saveFileDialog.FileName, package.GetAsByteArray());

                    MessageBox.Show("Export Successful!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }


        private void UpdateMonthNavigationButtons()
        {
            btnPrevMonth.IsEnabled =
              SalesService.GetPreviousArticleSalesMonth(_currentMonth) != null;

            btnNextMonth.IsEnabled =
              SalesService.GetNextArticleSalesMonth(_currentMonth) != null;

            btnPrevMonth.Opacity = btnPrevMonth.IsEnabled ? 1.0 : 0.4;
            btnNextMonth.Opacity = btnNextMonth.IsEnabled ? 1.0 : 0.4;
        }

        private void dgArticles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while (dep != null && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null) return;

            DataGridCell cell = (DataGridCell)dep;

            var row = cell.DataContext as ArticleSaleRow;
            if (row == null || row.IsTotalRow) return;
            if (cell.Column == null) return;

            string article = cell.Column.Header?.ToString();
            if (cell.Column.DisplayIndex == 0 || article == "Total" || string.IsNullOrEmpty(article))
                return;

            if (!row.ArticleTooltips.ContainsKey(article) ||
                string.IsNullOrWhiteSpace(row.ArticleTooltips[article]))
                return;

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


        private void LoadSalesTillNow()
{
    try
    {
        var raw = SalesService.GetArticleSalesTillNowRaw();
        if (raw == null) return;
        if (cmbSaleTillNowFilterCategory.SelectedItem != null &&
            cmbSaleTillNowFilterCategory.SelectedItem.ToString() != "All")
        {
            string selectedCategory = cmbSaleTillNowFilterCategory.SelectedItem.ToString();

            raw = raw.Where(x =>
                _articleCategoryMap.ContainsKey(x.Article) &&
                _articleCategoryMap[x.Article] == selectedCategory
            ).ToList();
        }
        if (cmbYearFilter.SelectedItem != null &&
            cmbYearFilter.SelectedItem.ToString() != "All")
        {
            int selectedYear = int.Parse(cmbYearFilter.SelectedItem.ToString());

            raw = raw.Where(x =>
            {
                if (DateTime.TryParse(x.Date, out DateTime dt))
                    return dt.Year == selectedYear;
                return false;
            }).ToList();
        }

        if (cmbMonthFilter.SelectedItem != null &&
            ((ComboBoxItem)cmbMonthFilter.SelectedItem).Content.ToString() != "All")
        {
            int selectedMonth =
                DateTime.ParseExact(
                    ((ComboBoxItem)cmbMonthFilter.SelectedItem).Content.ToString(),
                    "MMMM",
                    CultureInfo.InvariantCulture
                ).Month;

            raw = raw.Where(x =>
            {
                if (DateTime.TryParse(x.Date, out DateTime dt))
                    return dt.Month == selectedMonth;
                return false;
            }).ToList();
        }
        var articles = raw.Select(x => x.Article).Distinct().OrderBy(a => a).ToList();

        var totals = articles.ToDictionary(
            art => art,
            art => raw.Where(x => x.Article == art).Sum(x => x.Qty)
        );

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
            LoadSalesTillNow();
        }

        private void MonthFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            LoadSalesTillNow();
        }

        private void FilterCategory_ChangedSaleTillNow(object sender, SelectionChangedEventArgs e)
        {
            LoadSalesTillNow();
        }


        private void ViewSalesTillNowGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                var raw = SalesService.GetArticleSalesTillNowRaw();
                if (raw == null || !raw.Any())
                {
                    MessageBox.Show("No data available.");
                    return;
                }

                if (cmbSaleTillNowFilterCategory.SelectedItem != null &&
                    cmbSaleTillNowFilterCategory.SelectedItem.ToString() != "All")
                {
                    string selectedCategory = cmbSaleTillNowFilterCategory.SelectedItem.ToString();
                    raw = raw.Where(x =>
                        _articleCategoryMap.ContainsKey(x.Article) &&
                        _articleCategoryMap[x.Article] == selectedCategory
                    ).ToList();
                }
                if (cmbYearFilter.SelectedItem != null &&
                    cmbYearFilter.SelectedItem.ToString() != "All")
                {
                    int selectedYear = int.Parse(cmbYearFilter.SelectedItem.ToString());
                    raw = raw.Where(x =>
                    {
                        if (DateTime.TryParse(x.Date, out DateTime dt))
                            return dt.Year == selectedYear;
                        return false;
                    }).ToList();
                }

                if (cmbMonthFilter.SelectedItem != null &&
                    ((ComboBoxItem)cmbMonthFilter.SelectedItem).Content.ToString() != "All")
                {
                    int selectedMonth = DateTime.ParseExact(
                        ((ComboBoxItem)cmbMonthFilter.SelectedItem).Content.ToString(),
                        "MMMM",
                        CultureInfo.InvariantCulture
                    ).Month;

                    raw = raw.Where(x =>
                    {
                        if (DateTime.TryParse(x.Date, out DateTime dt))
                            return dt.Month == selectedMonth;
                        return false;
                    }).ToList();
                }

                if (!raw.Any())
                {
                    MessageBox.Show("No records found for the selected filters.");
                    return;
                }

                var graphData = raw
                    .GroupBy(x => x.Article)
                    .Select(g => new
                    {
                        Article = g.Key,
                        TotalQty = g.Sum(x => x.Qty)
                    })
                    .Where(x => x.TotalQty > 0) 
                    .ToDictionary(x => x.Article, x => (decimal)x.TotalQty);

              
                var graphWin = new BuyerGraphWindow(
                    graphData,
                    "Sales Analysis (Qty)", 
                    "Articles",            
                    true                    
                );

                graphWin.Owner = Window.GetWindow(this);
                graphWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating graph: " + ex.Message);
            }
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
        private void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            cmbFilterCategory.SelectedIndex = 0; 
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;

            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            txtTitle.Text = _currentMonth.ToString("MMMM yyyy");
            LoadArticleReport(_currentMonth);

            FilterPanel.Visibility = Visibility.Collapsed;
        }

        private void dgArticles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void ViewGraph_Click(object sender, RoutedEventArgs e)
        {
           
            var rows = dgArticles.ItemsSource as List<ArticleSaleRow>;
            if (rows == null || !rows.Any()) { MessageBox.Show("No data"); return; }
            var totalRow = rows.FirstOrDefault(r => r.IsTotalRow);
            if (totalRow == null) { MessageBox.Show("No totals found"); return; }
            var graphData = new Dictionary<string, decimal>();
            foreach (var kvp in totalRow.ArticleValues)
            {
                if (decimal.TryParse(kvp.Value, out decimal qty) && qty > 0)
                {
                    graphData[kvp.Key] = qty;
                }
            }

            if (graphData.Count == 0) { MessageBox.Show("Total quantity is zero."); return; }

            var graphWin = new BuyerGraphWindow(graphData, $"Article Qty: {txtTitle.Text}", "Articles", true);

            graphWin.Owner = Window.GetWindow(this);
            graphWin.ShowDialog();
        }
        // Inside SaleByBuyerPage class

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Force focus to the DataGrid so arrow keys work immediately
            dgArticles.Focus();
        }

        // Override the PreviewKeyDown to handle custom scrolling logic
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            var scrollViewer = GetScrollViewer(dgArticles);
            if (scrollViewer == null) return;

            switch (e.Key)
            {
                // Vertical
                case Key.Up: scrollViewer.LineUp(); break;
                case Key.Down: scrollViewer.LineDown(); break;

                // Horizontal (Side-wise)
                case Key.Left:
                    scrollViewer.LineLeft();
                    e.Handled = true; // Prevent shifting focus to sidebar
                    break;
                case Key.Right:
                    scrollViewer.LineRight();
                    e.Handled = true;
                    break;

                case Key.PageUp: scrollViewer.PageUp(); break;
                case Key.PageDown: scrollViewer.PageDown(); break;
            }
        }

        // Helper method to find the ScrollViewer inside the DataGrid
        private ScrollViewer GetScrollViewer(UIElement element)
        {
            if (element == null) return null;
            if (element is ScrollViewer viewer) return viewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i) as UIElement;
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
