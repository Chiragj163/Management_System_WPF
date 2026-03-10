
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
            LoadReport(_currentMonth); // ✅ FIX
        }



        private void LoadCategories()
        {
            var items = ItemsService.GetAllItems();

            // ItemName → Category
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
               // MessageBox.Show("No records found");
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

            // ==========================================
            // ✅ CALCULATE TOTALS AND SORT BUYERS
            // ==========================================

            // 1. Calculate the total for each buyer
            var buyerTotals = new Dictionary<string, decimal>();
            foreach (var buyer in buyers)
            {
                decimal colTotal = rows
                    .Where(r => r.BuyerValues.ContainsKey(buyer) && r.BuyerValues[buyer].HasValue)
                    .Sum(r => r.BuyerValues[buyer]!.Value);

                buyerTotals[buyer] = colTotal;
            }

            // 2. Sort the buyers list based on their totals (Highest to Lowest)
            var sortedBuyers = buyers.OrderByDescending(b => buyerTotals[b]).ToList();

            // 3. Build the Total Row using the calculated totals
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

            // ==========================================
            // BIND TO GRID
            // ==========================================
            dgBuyerSales.ItemsSource = null;
            dgBuyerSales.Columns.Clear();

            // 4. Pass the SORTED buyers to build the columns in the correct visual order
            BuildDynamicColumns(sortedBuyers);

            dgBuyerSales.ItemsSource = rows;
        }


        // LOAD REPORT (CURRENT MONTH / PREVIOUS MONTH)

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
            // 1️⃣ Reset flags
            _isRangeFilter = false;

            // 2️⃣ Reset month to current
            var now = DateTime.Now;
            _currentMonth = new DateTime(now.Year, now.Month, 1);

            // 3️⃣ Reset category
            cmbCategory.SelectedIndex = 0;

            // 4️⃣ Clear date pickers
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;

            // 5️⃣ Load default month report
            LoadReport(_currentMonth);

            // 6️⃣ Hide panel
            FilterPanel.Visibility = Visibility.Collapsed;

            // 7️⃣ Enable month navigation
            btnPrevMonth.IsEnabled = true;
            btnNextMonth.IsEnabled = true;
            btnPrevMonth.Opacity = 1;
            btnNextMonth.Opacity = 1;
        }


        // BUILD TABLE COLUMNS

        // 1. Paste this method inside SaleByBuyerPage class
        private void BuildDynamicColumns(List<string> buyers)
        {
            dgBuyerSales.Columns.Clear();

            // ================= DATE COLUMN =================
            var dateCol = new DataGridTextColumn
            {
                Header = "Dates",
                Binding = new Binding("Date") { Converter = new DateOrTotalConverter() },
                Width = 150,
                IsReadOnly = true
            };

            // ✅ Fix 1: Safely find the style. If missing, it won't crash.
            dateCol.CellStyle = dgBuyerSales.TryFindResource("StandardCellStyle") as Style;

            dgBuyerSales.Columns.Add(dateCol);
            // --- Buyer Columns (Dynamic) ---
            foreach (var buyer in buyers)
            {
                // Define the column variable properly
                var col = new DataGridTextColumn
                {
                    Header = buyer,
                    Binding = new Binding($"BuyerValues[{buyer}]") { StringFormat = "0.##" },
                    Width = 120,
                    IsReadOnly = true
                };

                // ✅ USE THE STYLE FROM XAML (ClickableCellStyle)
                // This ensures the EventSetter (Click) works AND the design stays beautiful
                col.CellStyle = dgBuyerSales.Resources["ClickableCellStyle"] as Style;

                dgBuyerSales.Columns.Add(col);
            }

            // --- Total Column ---
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

        // 2. Helper Method to get data
        private List<SaleDetailItem> GetDetailsForCell(DataGridCell cell)
        {
            if (cell.DataContext is not SaleByBuyerRow rowData) return null;

            // Use 'as' safely
            var col = cell.Column as DataGridTextColumn;
            if (col == null) return null;

            string buyerName = col.Header.ToString();

            if (buyerName == "Dates" || buyerName == "Total") return null;

            // Filter using cached raw data
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

        // 3. Click Event Handler
        private void OnCellDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            var cell = sender as DataGridCell;
            if (cell == null) return;

            List<SaleDetailItem> details = GetDetailsForCell(cell);

            if (details != null && details.Count > 0)
            {
                // Get Title Info safely
                string buyerName = cell.Column.Header.ToString();
                string dateStr = "";

                if (cell.DataContext is SaleByBuyerRow row)
                {
                    dateStr = row.Date == DateTime.MinValue ? "Total" : row.Date.ToString("dd MMM yyyy");
                }

                // Open the ItemDetailsWindow
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


        // BUTTON: PREVIOUS MONTH

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




        // BUTTON: EXPORT TO EXCEL (CSV format)

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the raw data source (Better than looping UI items)
            var rows = dgBuyerSales.ItemsSource as IEnumerable<SaleByBuyerRow>;
            if (rows == null || !rows.Any())
            {
                MessageBox.Show("Nothing to export!");
                return;
            }

            // 2. Configure Save Dialog for .xlsx
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"SalesByBuyer_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                // 3. Set License (Required for EPPlus 5+)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Sales Report");

                    // --- HEADERS ---

                    // Get all unique Buyer names from the data to ensure we have columns for everyone
                    var allBuyers = rows
      .Where(r => r?.BuyerValues != null)
      .SelectMany(r => r.BuyerValues.Keys)
      .Distinct()
      .OrderBy(x => x)
      .ToList();


                    // Setup Header Row
                    ws.Cells[1, 1].Value = "Date";
                    int col = 2;
                    foreach (var buyer in allBuyers)
                    {
                        ws.Cells[1, col++].Value = buyer;
                    }
                    // Add "Total" Header
                    ws.Cells[1, col].Value = "Total";

                    // Dictionary for Vertical Totals (Bottom Row)
                    var columnTotals = allBuyers.ToDictionary(b => b, b => 0m);
                    decimal grandTotal = 0;


                    // --- DATA ROWS ---
                    int rowIdx = 2;
                    foreach (var r in rows)
                    {
                        if (r.Date.Year < 1900) continue;
                        // Date Column
                        ws.Cells[rowIdx, 1].Value = r.Date;
                        ws.Cells[rowIdx, 1].Style.Numberformat.Format = "dd/mm/yyyy";
                        ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        col = 2;
                        decimal rowSum = 0; // Horizontal Total

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
                                columnTotals[buyer] += val; // Add to vertical total
                            }
                            else
                            {
                                ws.Cells[rowIdx, col].Value = 0;
                                ws.Cells[rowIdx, col].Style.Font.Color.SetColor(System.Drawing.Color.LightGray);
                            }
                            col++;
                        }

                        // Write Row Total (Last Column)
                        ws.Cells[rowIdx, col].Value = rowSum;
                        ws.Cells[rowIdx, col].Style.Font.Bold = true;
                        ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.WhiteSmoke);

                        grandTotal += rowSum;
                        rowIdx++;
                    }

                    // --- TOTAL ROW (BOTTOM) ---
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

                    // Bottom Right Grand Total
                    ws.Cells[rowIdx, col].Value = grandTotal;
                    ws.Cells[rowIdx, col].Style.Font.Bold = true;
                    ws.Cells[rowIdx, col].Style.Font.Size = 12;
                    ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // --- STYLING ---

                    // Header Style
                    var headerRange = ws.Cells[1, 1, 1, col];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Teal);
                    headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);

                    // Borders
                    var fullRange = ws.Cells[1, 1, rowIdx, col];
                    fullRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
fullRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
fullRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
fullRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    // AutoFit
                    ws.Cells.AutoFitColumns();
                    ws.View.FreezePanes(2, 1); // Freeze header

                    // Save
                    File.WriteAllBytes(dialog.FileName, package.GetAsByteArray());

                    // Ask to open
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


        // BUTTON: PRINT

        private void Filter_Category_Click(object sender, SelectionChangedEventArgs e)
        {
            // Re-apply current report with selected category
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
            // 1. Get the current data from the DataGrid
            var rows = dgBuyerSales.ItemsSource as List<SaleByBuyerRow>;

            if (rows == null || !rows.Any())
            {
                MessageBox.Show("No data available to graph.");
                return;
            }

            // 2. Find the "Grand Total" row (It has Date = DateTime.MinValue)
            var totalRow = rows.FirstOrDefault(r => r.Date == DateTime.MinValue);

            if (totalRow == null || totalRow.BuyerValues == null || !totalRow.BuyerValues.Any())
            {
                MessageBox.Show("No totals found.");
                return;
            }

            // 3. Convert to Dictionary<string, decimal> for the window
            // Only take buyers with > 0 sales
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

            // 4. Open the Graph Window
           
            var graphWin = new BuyerGraphWindow(graphData, $"Sales Analysis: {txtTitle.Text}", "Buyers", false);

            graphWin.Owner = Window.GetWindow(this);
            graphWin.ShowDialog();
        }

    }
   
}
