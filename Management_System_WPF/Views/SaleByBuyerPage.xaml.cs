
using Management_System_WPF.Helpers;
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
    public partial class SaleByBuyerPage : Page
    {
        private DateTime _filterFrom;
        private DateTime _filterTo;
        private bool _isRangeFilter = false;
        private Dictionary<string, string> _articleCategoryMap = new();
        private DateTime _currentMonth;
        private List<SaleRecord> _cachedRawSales = new();
        public SaleByBuyerPage()
        {
            InitializeComponent();





            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            LoadCategories();
            LoadReport(_currentMonth);
        }

        private void LoadCategories()
        {
            var items = ItemsService.GetAllItems();
            _articleCategoryMap = items
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .ToDictionary(i => i.Name, i => i.Category);

            var categories = new List<string> { "All" };
            categories.AddRange(
                items
                    .Select(i => i.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c)
            );

            cmbCategory.ItemsSource = categories;
            cmbCategory.SelectedIndex = 0;
        }
        private List<SaleRecord> ApplyCategoryFilter(List<SaleRecord> sales)
        {
            if (cmbCategory.SelectedItem == null ||
                cmbCategory.SelectedItem.ToString() == "All")
                return sales;

            string selectedCategory = cmbCategory.SelectedItem.ToString();

            return sales.Where(s =>
                _articleCategoryMap.ContainsKey(s.ItemName) &&
                _articleCategoryMap[s.ItemName] == selectedCategory
            ).ToList();
        }

        private void BuildBuyerReport(List<SaleRecord> sales, string title)

        {
            if (!sales.Any())
            {
                dgBuyerSales.ItemsSource = null;
                dgBuyerSales.Columns.Clear();
                txtTitle.Text = title;
                return;
            }

            txtTitle.Text = title;

            var buyers = sales
                .Select(s => s.BuyerName?.Trim())
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var dates = sales
                .Select(s => s.SaleDate.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var rows = new List<SaleByBuyerRow>();

            sales = sales
                .Where(s => !string.IsNullOrWhiteSpace(s.BuyerName))
                .ToList();

            foreach (var date in dates)
            {
                var row = new SaleByBuyerRow { Date = date };
                decimal rowTotal = 0m;

                foreach (var buyer in buyers)
                {
                    decimal total = sales
                        .Where(x => x.BuyerName!.Trim().Equals(buyer, StringComparison.OrdinalIgnoreCase)
                                 && x.SaleDate.Date == date)
                        .Sum(x => x.Qty * x.Price);

                    row.BuyerValues[buyer] = total > 0 ? total : null;
                    if (total > 0) rowTotal += total;
                }

                row.Total = rowTotal > 0 ? rowTotal : null;
                if (rowTotal > 0)
                {
                    row.Total = rowTotal;
                    rows.Add(row);
                }

            }


            var buyerTotals = new Dictionary<string, decimal>();
            foreach (var buyer in buyers)
            {
                decimal colTotal = rows
                    .Where(r => r.BuyerValues.ContainsKey(buyer) && r.BuyerValues[buyer].HasValue)
                    .Sum(r => r.BuyerValues[buyer]!.Value);

                buyerTotals[buyer] = colTotal;
            }


            var sortedBuyers = buyers.OrderByDescending(b => buyerTotals[b]).ToList();
            var totalRow = new SaleByBuyerRow { Date = DateTime.MinValue };
            decimal grandTotal = 0m;

            foreach (var buyer in sortedBuyers)
            {
                decimal colTotal = buyerTotals[buyer];
                totalRow.BuyerValues[buyer] = colTotal > 0 ? colTotal : null;
                grandTotal += colTotal;
            }

            totalRow.Total = grandTotal;
            rows.Add(totalRow);

            dgBuyerSales.ItemsSource = null;
            dgBuyerSales.Columns.Clear();
            BuildDynamicColumns(sortedBuyers);

            dgBuyerSales.ItemsSource = rows;
        }

        private void LoadReport(DateTime month)
        {
            _isRangeFilter = false;

            var sales = SalesService.GetSales()
                .Where(s => s.SaleDate.Month == month.Month &&
                            s.SaleDate.Year == month.Year)
                .ToList();
            _cachedRawSales = ApplyCategoryFilter(sales);
            sales = ApplyCategoryFilter(sales);
            BuildBuyerReport(sales, month.ToString("MMMM yyyy"));
            UpdateMonthButtons();
        }

        private void LoadReportByRange(DateTime from, DateTime to)
        {
            _isRangeFilter = true;
            _filterFrom = from;
            _filterTo = to;

            var sales = SalesService.GetSales()
                .Where(s => s.SaleDate.Date >= from.Date &&
                            s.SaleDate.Date <= to.Date)
                .ToList();

            sales = ApplyCategoryFilter(sales);

            BuildBuyerReport(
                sales,
                $"{from:dd MMM yyyy} - {to:dd MMM yyyy}"
            );
        }

        private void FilterThisMonth_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            _currentMonth = new DateTime(now.Year, now.Month, 1);
            LoadReport(_currentMonth);
            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterSixMonths_Click(object sender, RoutedEventArgs e)
        {
            LoadReportByRange(DateTime.Today.AddMonths(-6), DateTime.Today);
            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterYear_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            LoadReportByRange(
                new DateTime(now.Year, 1, 1),
                new DateTime(now.Year, 12, 31)
            );
            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterCustom_Click(object sender, RoutedEventArgs e)
        {
            if (!dpFrom.SelectedDate.HasValue || !dpTo.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select both dates");
                return;
            }

            LoadReportByRange(dpFrom.SelectedDate.Value, dpTo.SelectedDate.Value);
            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility =
                FilterPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {

            _isRangeFilter = false;


            var now = DateTime.Now;
            _currentMonth = new DateTime(now.Year, now.Month, 1);

            cmbCategory.SelectedIndex = 0;
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;

            LoadReport(_currentMonth);
            FilterPanel.Visibility = Visibility.Collapsed;
            btnPrevMonth.IsEnabled = true;
            btnNextMonth.IsEnabled = true;
            btnPrevMonth.Opacity = 1;
            btnNextMonth.Opacity = 1;
        }


        // BUILD TABLE COLUMNS
        private void BuildDynamicColumns(List<string> buyers)
        {
            dgBuyerSales.Columns.Clear();
            var dateCol = new DataGridTextColumn
            {
                Header = "Dates",
                Binding = new Binding("Date") { Converter = new DateOrTotalConverter() },
                Width = 150,
                IsReadOnly = true
            };
            dateCol.CellStyle = dgBuyerSales.TryFindResource("StandardCellStyle") as Style;

            dgBuyerSales.Columns.Add(dateCol);
            foreach (var buyer in buyers)
            {
                var col = new DataGridTextColumn
                {
                    Header = buyer,
                    Binding = new Binding($"BuyerValues[{buyer}]") { StringFormat = "0.##" },
                    Width = 120,
                    IsReadOnly = true
                };
                col.CellStyle = dgBuyerSales.Resources["ClickableCellStyle"] as Style;

                dgBuyerSales.Columns.Add(col);
            }

            dgBuyerSales.Columns.Add(new DataGridTextColumn
            {
                Header = "Total",
                Binding = new Binding("Total") { StringFormat = "0.##" },
                Width = 140,
                IsReadOnly = true,
                CellStyle = new Style(typeof(DataGridCell))
                {
                    Setters =
            {
                new Setter(DataGridCell.BackgroundProperty, Brushes.LightGreen),
                new Setter(DataGridCell.FontWeightProperty, FontWeights.Bold),
                new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                new Setter(DataGridCell.BorderThicknessProperty, new Thickness(1)),
                new Setter(DataGridCell.BorderBrushProperty, Brushes.Gray),
                new Setter(DataGridCell.MarginProperty, new Thickness(1.5))
            }
                }
            });
        }

        private List<SaleDetailItem> GetDetailsForCell(DataGridCell cell)
        {
            if (cell.DataContext is not SaleByBuyerRow rowData) return null;

            var col = cell.Column as DataGridTextColumn;
            if (col == null) return null;

            string buyerName = col.Header.ToString();

            if (buyerName == "Dates" || buyerName == "Total") return null;

            var relevantSales = _cachedRawSales
                .Where(s =>
                    s.SaleDate.Date == rowData.Date &&
                    (s.BuyerName ?? "").Trim().Equals(buyerName.Trim(), StringComparison.OrdinalIgnoreCase)
                )
                .Select(s => new SaleDetailItem
                {
                    Article = s.ItemName,
                    Qty = s.Qty,
                    Price = s.Price
                })
                .ToList();

            return relevantSales;
        }

        private void OnCellDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            var cell = sender as DataGridCell;
            if (cell == null) return;

            List<SaleDetailItem> details = GetDetailsForCell(cell);

            if (details != null && details.Count > 0)
            {
                string buyerName = cell.Column.Header.ToString();
                string dateStr = "";

                if (cell.DataContext is SaleByBuyerRow row)
                {
                    dateStr = row.Date == DateTime.MinValue ? "Total" : row.Date.ToString("dd MMM yyyy");
                }

                var popup = new ItemDetailsWindow(buyerName, dateStr, details);
                popup.Owner = Window.GetWindow(this);
                popup.ShowDialog();
            }
        }

        public class DateOrTotalConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value == null || value == DependencyProperty.UnsetValue)
                    return "";

                if (value is DateTime dt)
                {
                    if (dt == DateTime.MinValue)
                        return "Total";

                    return dt.ToString("dd/MM/yyyy");
                }

                return "";
            }


            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }


        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var prev = SalesService.GetPreviousSalesMonth(_currentMonth);

            if (prev != null)
            {
                _currentMonth = prev.Value;
                LoadReport(_currentMonth);
            }
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            var next = SalesService.GetNextSalesMonth(_currentMonth);

            if (next != null)
            {
                _currentMonth = next.Value;
                LoadReport(_currentMonth);
            }
        }



        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {

            var rows = dgBuyerSales.ItemsSource as IEnumerable<SaleByBuyerRow>;
            if (rows == null || !rows.Any())
            {
                MessageBox.Show("Nothing to export!");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"SalesByBuyer_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Sales Report");

                    var allBuyers = rows
      .Where(r => r?.BuyerValues != null)
      .SelectMany(r => r.BuyerValues.Keys)
      .Distinct()
      .OrderBy(x => x)
      .ToList();



                    ws.Cells[1, 1].Value = "Date";
                    int col = 2;
                    foreach (var buyer in allBuyers)
                    {
                        ws.Cells[1, col++].Value = buyer;
                    }

                    ws.Cells[1, col].Value = "Total";


                    var columnTotals = allBuyers.ToDictionary(b => b, b => 0m);
                    decimal grandTotal = 0;



                    int rowIdx = 2;
                    foreach (var r in rows)
                    {
                        if (r.Date.Year < 1900) continue;
                        // Date Column
                        ws.Cells[rowIdx, 1].Value = r.Date;
                        ws.Cells[rowIdx, 1].Style.Numberformat.Format = "dd/mm/yyyy";
                        ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        col = 2;
                        decimal rowSum = 0;

                        foreach (var buyer in allBuyers)
                        {
                            decimal val = 0;
                            if (r.BuyerValues.ContainsKey(buyer) && r.BuyerValues[buyer].HasValue)
                            {
                                val = (decimal)r.BuyerValues[buyer].Value;
                            }

                            if (val > 0)
                            {
                                ws.Cells[rowIdx, col].Value = val;
                                rowSum += val;
                                columnTotals[buyer] += val;
                            }
                            else
                            {
                                ws.Cells[rowIdx, col].Value = 0;
                                ws.Cells[rowIdx, col].Style.Font.Color.SetColor(System.Drawing.Color.LightGray);
                            }
                            col++;
                        }


                        ws.Cells[rowIdx, col].Value = rowSum;
                        ws.Cells[rowIdx, col].Style.Font.Bold = true;
                        ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.WhiteSmoke);

                        grandTotal += rowSum;
                        rowIdx++;
                    }


                    ws.Cells[rowIdx, 1].Value = " TOTAL";
                    ws.Cells[rowIdx, 1].Style.Font.Bold = true;

                    col = 2;
                    foreach (var buyer in allBuyers)
                    {
                        ws.Cells[rowIdx, col].Value = columnTotals[buyer];
                        ws.Cells[rowIdx, col].Style.Font.Bold = true;
                        ws.Cells[rowIdx, col].Style.Border.Top.Style = ExcelBorderStyle.Medium;
                        col++;
                    }
                    ws.Cells[rowIdx, col].Value = grandTotal;
                    ws.Cells[rowIdx, col].Style.Font.Bold = true;
                    ws.Cells[rowIdx, col].Style.Font.Size = 12;
                    ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    var headerRange = ws.Cells[1, 1, 1, col];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Teal);
                    headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);

                    var fullRange = ws.Cells[1, 1, rowIdx, col];
                    fullRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    fullRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    fullRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    fullRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    ws.Cells.AutoFitColumns();
                    ws.View.FreezePanes(2, 1);

                    File.WriteAllBytes(dialog.FileName, package.GetAsByteArray());
                    if (MessageBox.Show("Export Successful! Open file now?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dialog.FileName, UseShellExecute = true });
                    }
                }
            }
            catch (IOException)
            {
                MessageBox.Show("The file is open in Excel. Please close it and try again.", "File Locked", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting: " + ex.Message);
            }
        }

        private void Filter_Category_Click(object sender, SelectionChangedEventArgs e)
        {
            if (_isRangeFilter)
            {
                LoadReportByRange(_filterFrom, _filterTo);
            }
            else
            {
                LoadReport(_currentMonth);
            }
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
        private void UpdateMonthButtons()
        {
            btnPrevMonth.IsEnabled = SalesService.GetPreviousSalesMonth(_currentMonth) != null;
            btnNextMonth.IsEnabled = SalesService.GetNextSalesMonth(_currentMonth) != null;

            btnPrevMonth.Opacity = btnPrevMonth.IsEnabled ? 1.0 : 0.4;
            btnNextMonth.Opacity = btnNextMonth.IsEnabled ? 1.0 : 0.4;
        }
        private void ViewGraph_Click(object sender, RoutedEventArgs e)
        {

            var rows = dgBuyerSales.ItemsSource as List<SaleByBuyerRow>;

            if (rows == null || !rows.Any())
            {
                MessageBox.Show("No data available to graph.");
                return;
            }


            var totalRow = rows.FirstOrDefault(r => r.Date == DateTime.MinValue);

            if (totalRow == null || totalRow.BuyerValues == null || !totalRow.BuyerValues.Any())
            {
                MessageBox.Show("No totals found.");
                return;
            }

            var graphData = new Dictionary<string, decimal>();

            foreach (var kvp in totalRow.BuyerValues)
            {
                if (kvp.Value.HasValue && kvp.Value.Value > 0)
                {
                    graphData[kvp.Key] = kvp.Value.Value;
                }
            }

            if (graphData.Count == 0)
            {
                MessageBox.Show("Total sales are zero.");
                return;
            }



            var graphWin = new BuyerGraphWindow(graphData, $"Sales Analysis: {txtTitle.Text}", "Buyers", false);

            graphWin.Owner = Window.GetWindow(this);
            graphWin.ShowDialog();
        }
        // Inside SaleByBuyerPage class

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Force focus to the DataGrid so arrow keys work immediately
            dgBuyerSales.Focus();
        }

        // Override the PreviewKeyDown to handle custom scrolling logic
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            var scrollViewer = GetScrollViewer(dgBuyerSales);
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