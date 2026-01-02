using Management_System_WPF.Helpers;
using Management_System_WPF.Models;
using Management_System_WPF.Services;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Management_System_WPF.Views
{
    public partial class BuyerReportPage : Page
    {
        private readonly int buyerId;
        private DateTime _currentMonth;

        private decimal _currentTotalSales;
        private decimal _currentPayment;

        private List<SalesRaw> _cachedSales = new();
        private List<SalesRaw> _cachedReturns = new();

        public BuyerReportPage(int id, string buyerName = "")
        {
            InitializeComponent();

            buyerId = id;
            if (!string.IsNullOrWhiteSpace(buyerName))
                txtBuyerName.Text = buyerName;


            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            LoadBuyerData();
        }

        // =========================================================
        // MAIN LOAD (MONTH MODE)
        // =========================================================
        private void LoadBuyerData()
        {
            ReturnService.Initialize();
            PaymentService.Initialize();

            // 1. Fetch Sales
            _cachedSales = SalesService.GetSalesRawForPivot(buyerId, _currentMonth.Year, _currentMonth.Month)
                           ?? new List<SalesRaw>();

            // 2. Fetch Returns
            _cachedReturns = ReturnService.GetReturnsForPivot(buyerId, _currentMonth.Year, _currentMonth.Month)
                             ?? new List<SalesRaw>();

            // 3. Handle Empty State (Exit only if BOTH are empty)
            if (_cachedSales.Count == 0 && _cachedReturns.Count == 0)
            {
                dgBuyerReport.ItemsSource = null;
                txtMonthName.Text = _currentMonth.ToString("MMMM yyyy");
                UpdateFooter(0, 0);
                UpdateMonthNavigationButtons();
                return;
            }

            // 4. Create Pivot Table (Sales + Returns)
            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(_cachedSales, _cachedReturns);

            BindPivotToGrid(pivot);
            CalculateMonthlyTotals();

            txtMonthName.Text = _currentMonth.ToString("MMMM yyyy");
            UpdateMonthNavigationButtons();
        }

        // =========================================================
        // TOTALS
        // =========================================================
        private void CalculateMonthlyTotals()
        {
            // 1. Gross Sales
            decimal grossSales = _cachedSales.Sum(x => x.Qty * x.Price);

            // 2. Return Value (FIXED: Look up price if not sold this month)
            var allDbItems = ItemsService.GetAllItems();

            decimal totalReturnValue = _cachedReturns.Sum(ret =>
            {
                // Try sales price
                var sale = _cachedSales.FirstOrDefault(s => s.ItemName == ret.ItemName);
                decimal price = sale?.Price ?? 0;

                // Fallback to DB price
                if (price == 0)
                {
                    var dbItem = allDbItems.FirstOrDefault(i => i.Name == ret.ItemName);
                    if (dbItem != null) price = dbItem.Price;
                }

                return ret.Qty * price;
            });

            // 3. Net Sales
            _currentTotalSales = grossSales - totalReturnValue;

            // 4. Payment
            _currentPayment = PaymentService.GetPayment(
                buyerId,
                _currentMonth.Year,
                _currentMonth.Month
            );

            UpdateFooter(_currentTotalSales, _currentPayment);
        }

        private void UpdateFooter(decimal sales, decimal payment)
        {
            txtTotalSales.Text = $"₹ {sales:N2}";
            txtPayment.Text = $"₹ {payment:N2}";
            txtBalance.Text = $"₹ {sales - payment:N2}";
        }

        // =========================================================
        // RETURN
        // =========================================================
       private void Return_Click(object sender, RoutedEventArgs e)
        {
            // Pass empty list or cached list, ReturnWindow now loads all items internally
            var win = new ReturnWindow(_cachedSales); 
            
            if (win.ShowDialog() == true)
            {
                ReturnService.AddReturn(
                    buyerId,
                    win.SelectedItemId,
                    _currentMonth.Year,
                    _currentMonth.Month,
                    win.ReturnQty
                );

                // Refresh Data
                LoadBuyerData();
                MessageBox.Show("Return added successfully.");
            }
        }
        // =========================================================
        // PAYMENT
        // =========================================================
        private void Payment_Click(object sender, RoutedEventArgs e)
        {
            var win = new PaymentWindow(_currentPayment);
            if (win.ShowDialog() == true)
            {
                PaymentService.SavePayment(
                    buyerId,
                    _currentMonth.Year,
                    _currentMonth.Month,
                    win.Amount
                );

                LoadBuyerData();
            }
        }

        // =========================================================
        // FILTERS
        // =========================================================
        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility =
                FilterPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void FilterThisMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            LoadBuyerData();
            FilterPanel.Visibility = Visibility.Collapsed;
        }

        private void FilterSixMonths_Click(object sender, RoutedEventArgs e)
        {
            ApplyDateRangeFilter(
                DateTime.Today.AddMonths(-6),
                DateTime.Today,
                "Last 6 Months"
            );
        }

        private void FilterYear_Click(object sender, RoutedEventArgs e)
        {
            ApplyDateRangeFilter(
                new DateTime(DateTime.Now.Year, 1, 1),
                DateTime.Today,
                $"{DateTime.Now.Year}"
            );
        }

        private void FilterCustom_Click(object sender, RoutedEventArgs e)
        {
            if (dpFrom.SelectedDate == null || dpTo.SelectedDate == null)
            {
                MessageBox.Show("Please select both From and To dates.");
                return;
            }

            ApplyDateRangeFilter(
                dpFrom.SelectedDate.Value,
                dpTo.SelectedDate.Value,
                "Custom Range"
            );
        }

        private void ApplyDateRangeFilter(DateTime from, DateTime to, string label)
        {
            var raw = SalesService.GetSalesBetweenDates(buyerId, from, to)
                      ?? new List<SalesRaw>();

            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(
                raw,
                new List<SalesRaw>()
            );

            BindPivotToGrid(pivot);

            _currentTotalSales = raw.Sum(x => x.Qty * x.Price);
            _currentPayment = 0;

            UpdateFooter(_currentTotalSales, _currentPayment);
            txtMonthName.Text = label;

            btnPrevMonth.IsEnabled = false;
            btnNextMonth.IsEnabled = false;
            FilterPanel.Visibility = Visibility.Collapsed;
        }

        // =========================================================
        // GRID
        // =========================================================
        private void BindPivotToGrid(DataTable pivot)
        {
            dgBuyerReport.Columns.Clear();

            double dateColumnWidth = 150; // keep your date width

            foreach (DataColumn col in pivot.Columns)
            {
                if (col.ColumnName.Equals("Date", StringComparison.OrdinalIgnoreCase))
                {
                    dgBuyerReport.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Date",
                        Binding = new Binding("[Date]"),   // ✅ FIXED binding
                        FontSize = 16,
                        IsReadOnly = true,
                        Width = new DataGridLength(dateColumnWidth, DataGridLengthUnitType.Pixel)
                    });
                }
                else
                {
                    dgBuyerReport.Columns.Add(new DataGridTextColumn
                    {
                        Header = col.ColumnName,
                        Binding = new Binding($"[{col.ColumnName}]"), // ✅ FIXED binding
                        FontSize = 16,
                        IsReadOnly = true,
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                    });
                }
            }

            dgBuyerReport.ItemsSource = pivot.DefaultView;
        }


        // =========================================================
        // MONTH NAVIGATION
        // =========================================================
        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var prev = SalesService.GetPreviousSalesMonthForBuyer(buyerId, _currentMonth);
            if (prev != null)
            {
                _currentMonth = prev.Value;
                LoadBuyerData();
            }
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            var next = SalesService.GetNextSalesMonthForBuyer(buyerId, _currentMonth);
            if (next != null)
            {
                _currentMonth = next.Value;
                LoadBuyerData();
            }
        }

        private void UpdateMonthNavigationButtons()
        {
            btnPrevMonth.IsEnabled =
                SalesService.GetPreviousSalesMonthForBuyer(buyerId, _currentMonth) != null;

            btnNextMonth.IsEnabled =
                SalesService.GetNextSalesMonthForBuyer(buyerId, _currentMonth) != null;

            btnPrevMonth.Opacity = btnPrevMonth.IsEnabled ? 1.0 : 0.4;
            btnNextMonth.Opacity = btnNextMonth.IsEnabled ? 1.0 : 0.4;
        }


        // =========================================================
        // PRINT
        // =========================================================
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var pd = new PrintDialog();
            if (pd.ShowDialog() != true)
                return;

            // Get printer page size
            double pageWidth = pd.PrintableAreaWidth;
            double pageHeight = pd.PrintableAreaHeight;

            // Build a document specifically for printing
            FlowDocument doc = BuildInvoiceDocumentForPrint(pageWidth, pageHeight);

            pd.PrintDocument(
                ((IDocumentPaginatorSource)doc).DocumentPaginator,
                $"Report - {txtBuyerName.Text}"
            );
        }

        private FlowDocument BuildInvoiceDocumentForPrint(double pageWidth, double pageHeight)
        {
            // Create a new FlowDocument dedicated to print layout
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Calibri"),
                FontSize = 10,
                TextAlignment = TextAlignment.Left,
                PageWidth = pageWidth,
                PageHeight = pageHeight,
                PagePadding = new Thickness(30),
                ColumnGap = 0,
                ColumnWidth = pageWidth - 60 // width minus left/right padding
            };

            // ============ 1. TITLE & SUBTITLE ============
            var buyerName = (txtBuyerName.Text ?? string.Empty).Trim();
            var monthName = (txtMonthName.Text ?? string.Empty).Trim();

            var titlePara = new Paragraph(new Run(buyerName.ToUpperInvariant()))
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            doc.Blocks.Add(titlePara);

            var subHeader = new Paragraph(
                new Run($"{monthName} | Generated: {DateTime.Now:dd-MM-yyyy}")
            )
            {
                FontSize = 12,
                Foreground = Brushes.Gray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(subHeader);
           
            // ============ 2. FETCH & GROUP DATA ============
            var sales = SalesService.GetSalesBetweenDates(
                buyerId,
                _currentMonth,
                _currentMonth.AddMonths(1).AddDays(-1)
            ) ?? new List<SalesRaw>();

            var returnsList = ReturnService.GetReturnsForPivot(
                buyerId,
                _currentMonth.Year,
                _currentMonth.Month
            ) ?? new List<SalesRaw>();

            if (!sales.Any() && !returnsList.Any())
            {
                doc.Blocks.Add(new Paragraph(new Run("No sales or returns for this month.")));
                return doc;
            }
           

            // ✅ SAFE dates from both sales + returns
            var dates = new List<DateTime>();
            foreach (var s in sales)
            {
                if (DateTime.TryParse(s.Date, out DateTime saleDate))
                    dates.Add(saleDate.Date);
            }
            foreach (var r in returnsList)
            {
                if (DateTime.TryParse(r.Date, out DateTime retDate))
                    dates.Add(retDate.Date);
            }
            dates = dates.Distinct().OrderBy(d => d).ToList();
            var returnTotals = returnsList
                
                 .GroupBy(r => r.ItemName.Trim())// ← NORMALIZE: trim + uppercase
                .ToDictionary(g => g.Key.ToUpperInvariant(), g => g.Sum(x => x.Qty));

            // 4. Group by item WITH NET TOTALS AND DateQty (SAFE parsing)
            // Get ALL unique items from sales OR returns (FIXED)
            var allItemNames = sales.Select(s => s.ItemName)
                .Concat(returnsList.Select(r => r.ItemName))
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            var items = allItemNames.Select(itemName =>
            {
                // Sales data (0 if no sales)
                var salesForItem = sales.Where(s => s.ItemName == itemName);
                decimal unitPrice =
    salesForItem.Any()
        ? salesForItem.First().Price
        : SalesService.GetItemPriceFromMaster(itemName);


                int totalQty = salesForItem.Sum(s => s.Qty);

                // Returns data
                int totalReturns = returnTotals.TryGetValue(
    itemName.Trim().ToUpperInvariant(), out int r
) ? r : 0;

                int netQty = totalQty - totalReturns;
                decimal netAmount = netQty * unitPrice;

                // DateQty (sales only)
                var dateQty = new Dictionary<DateTime, int>();
                foreach (var sale in salesForItem)
                {
                    if (DateTime.TryParse(sale.Date, out DateTime saleDate))
                    {
                        var dateKey = saleDate.Date;
                        dateQty[dateKey] = dateQty.TryGetValue(dateKey, out int existing)
                            ? existing + sale.Qty
                            : sale.Qty;
                    }
                }

                return new
                {
                    Item = itemName,
                    UnitPrice = unitPrice,
                    TotalQty = totalQty,
                    TotalReturns = totalReturns,
                    NetQty = netQty,
                    TotalAmount = netAmount,
                    DateQty = dateQty
                };
            }).Where(i => i.TotalQty > 0 || i.TotalReturns > 0) // only active items
              .ToList();






            // ============ 3. TABLE SETUP ============
            var table = new Table
            {
                CellSpacing = 0
            };
            doc.Blocks.Add(table);

            // Ensure column collection initialized
            table.Columns.Clear();

            // Date column (fixed width)
            const double dateColumnWidth = 90; // adjust as needed
            table.Columns.Add(new TableColumn { Width = new GridLength(dateColumnWidth) });

            // Item columns: equally share remaining width
            double remainingWidth = Math.Max(0, doc.ColumnWidth - dateColumnWidth);
            double perItemWidth = items.Count > 0 ? remainingWidth / items.Count : remainingWidth;

            foreach (var _ in items)
            {
                table.Columns.Add(new TableColumn
                {
                    Width = new GridLength(perItemWidth)
                });
            }

            // ============ 4. TABLE HEADER ============
            var headerGroup = new TableRowGroup();
            table.RowGroups.Add(headerGroup);

            var headerRow = new TableRow();
            headerGroup.Rows.Add(headerRow);

            AddExcelCell(headerRow, "Date", isBold: true, isHeader: true);

            foreach (var item in items)
            {
                AddExcelCell(headerRow, item.Item, isBold: true, isHeader: true);
            }

            // ============ 5. TABLE BODY ============
            var body = new TableRowGroup();
            table.RowGroups.Add(body);

            foreach (var date in dates)
            {
                var row = new TableRow();
                body.Rows.Add(row);

                string dateStr = date.ToString("dd-MM-yyyy");
                AddExcelCell(row, dateStr, isBold: false, isHeader: false);

                foreach (var item in items)
                {
                    string qty = item.DateQty.TryGetValue(date, out int q) ? q.ToString() : string.Empty;
                    AddExcelCell(row, qty, isBold: false, isHeader: false);
                }
            }

            // Spacer
            body.Rows.Add(new TableRow());
            // Return Qty row (between Total Qty and Unit Price)
            var returnRow = new TableRow();
            body.Rows.Add(returnRow);

            AddExcelCell(returnRow, "Return", isBold: true, isHeader: true);

            foreach (var item in items)
            {
                int returnQty = returnTotals.TryGetValue(item.Item.Trim().ToUpperInvariant(), out var rq)
                    ? rq : 0;
                string text = returnQty == 0 ? string.Empty : $"-{returnQty}";
                AddExcelCell(returnRow, text, isBold: true, isHeader: false);
            }

            // Total Qty row
            var qtyRow = new TableRow();
            body.Rows.Add(qtyRow);

            AddExcelCell(qtyRow, "Total Qty", isBold: true, isHeader: true);
            foreach (var item in items)
            {
                AddExcelCell(qtyRow, item.TotalQty.ToString(), isBold: true, isHeader: false);
            }
           

           
            // Unit Price row
            var priceRow = new TableRow();
            body.Rows.Add(priceRow);
            AddExcelCell(priceRow, "Unit Price", isBold: true, isHeader: true);
            foreach (var item in items)
            {
                AddExcelCell(priceRow, item.UnitPrice.ToString("X 0.00"), isBold: false, isHeader: false);
            }
            // Total Amount row (now shows NET totals)
            var amountRow = new TableRow();
            body.Rows.Add(amountRow);
            AddExcelCell(amountRow, "Total", isBold: true, isHeader: true);
            foreach (var item in items)
            {
                AddExcelCell(amountRow, item.TotalAmount.ToString("₹0.00"), isBold: true, isHeader: false);
            }


            // ============ 6. GRAND TOTAL ============
            // ===== GRAND TOTAL ROW (1 label + 1 big value cell) =====
            // Grand Total (now sums NET totals ✓)
            decimal grandTotal = items.Sum(x => x.TotalAmount);  // FIXED: now correct net total


            var grandTotalRow = new TableRow();
            body.Rows.Add(grandTotalRow);

            int totalColumns = 1 + items.Count;

            // 1) First column: "Grand Total" label
            var labelCell = new TableCell(new Paragraph(new Run("Grand Total")))
            {
                TextAlignment = TextAlignment.Center,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.8),
                Padding = new Thickness(6, 4, 6, 4),
                Background = Brushes.DarkGreen,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            grandTotalRow.Cells.Add(labelCell);

            // 2) ONE BIG CELL spanning all remaining columns for the value
            var valueCell = new TableCell(new Paragraph(new Run($"₹ {grandTotal:0,0.00}")))
            {
                TextAlignment = TextAlignment.Center,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.8),
                Padding = new Thickness(6, 4, 6, 4),
                Background = Brushes.DarkGreen,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                ColumnSpan = totalColumns - 1  // spans ALL remaining columns
            };
            grandTotalRow.Cells.Add(valueCell);


            return doc;
        }

        private void AddExcelCell(TableRow row, string text, bool isBold, bool isHeader)
        {
            var p = new Paragraph(new Run(text ?? string.Empty))
            {
                Margin = new Thickness(0),
                TextAlignment = TextAlignment.Center,
                FontSize = 11,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal
            };

            var cell = new TableCell(p)
            {
                TextAlignment = TextAlignment.Center,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.8),
                Padding = new Thickness(6, 4, 6, 4)
            };

            if (isHeader)
                cell.Background = Brushes.LightCyan;

            row.Cells.Add(cell);
        }

        // =========================================================
        // EXCEL
        // =========================================================
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (dgBuyerReport.ItemsSource is not DataView dv || dv.Count == 0)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Buyer Report");

            DataTable dt = dv.ToTable();

            for (int c = 0; c < dt.Columns.Count; c++)
            {
                ws.Cells[1, c + 1].Value = dt.Columns[c].ColumnName;
                ws.Cells[1, c + 1].Style.Font.Bold = true;
            }

            for (int r = 0; r < dt.Rows.Count; r++)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    ws.Cells[r + 2, c + 1].Value = dt.Rows[r][c];
                }
            }

            ws.Cells.AutoFitColumns();

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"BuyerReport_{DateTime.Now:yyyyMMdd}.xlsx"
            );

            File.WriteAllBytes(path, package.GetAsByteArray());
            MessageBox.Show("Exported to Desktop.");
        }

        // =========================================================
        // MENU + BACK
        // =========================================================
        private void ToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            if (ActionButtonsPanel.Visibility == Visibility.Visible)
            {
                ActionButtonsPanel.Visibility = Visibility.Collapsed;
                btnMenu.Content = "☰ Menu";
            }
            else
            {
                ActionButtonsPanel.Visibility = Visibility.Visible;
                btnMenu.Content = "✖ Close";
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


    }
}
