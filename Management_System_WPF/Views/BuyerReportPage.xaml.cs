using Management_System_WPF.Helpers;
using Management_System_WPF.Models;
using Management_System_WPF.Services;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
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
            dgBuyerReport.AutoGeneratingColumn += DgBuyerReport_AutoGeneratingColumn;
            buyerId = id;
            if (!string.IsNullOrWhiteSpace(buyerName))
                txtBuyerName.Text = buyerName;


            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            LoadBuyerData();
            LoadCategories();

        }
        private void LoadCategories()
        {
            try
            {
                var items = ItemsService.GetAllItems();

                var categories = new List<string> { "All" };
                categories.AddRange(items.Select(x => x.Category)
                                         .Distinct()
                                         .OrderBy(x => x));

                cmbCategory.ItemsSource = categories;
                cmbCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading categories: " + ex.Message);
            }
        }
        private void CategoryFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyCategoryFilter();
        }
        private void ApplyCategoryFilter()
        {
            if (cmbCategory.SelectedItem == null) return;

            string selectedCategory = cmbCategory.SelectedItem.ToString();

            // Rebuild main month view (sales + returns)
            var sales = _cachedSales;
            var returns = _cachedReturns;

            // Filter by CATEGORY
            if (selectedCategory != "All")
            {
                var allItems = ItemsService.GetAllItems()
                               .ToDictionary(i => i.Name, i => i.Category);

                sales = sales.Where(s =>
                    allItems.ContainsKey(s.ItemName) &&
                    allItems[s.ItemName] == selectedCategory
                ).ToList();

                returns = returns.Where(r =>
                    allItems.ContainsKey(r.ItemName) &&
                    allItems[r.ItemName] == selectedCategory
                ).ToList();
            }

            // Create new filtered pivot
            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(sales, returns);

            BindPivotToGrid(pivot);

            // Update totals
            decimal total = sales.Sum(x => x.Qty * x.Price);
            decimal returnTotal = returns.Sum(r => r.Qty * SalesService.GetItemPriceFromMaster(r.ItemName));

            decimal net = total - returnTotal;

            txtTotalSales.Text = $"₹ {net:N2}";
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
            ApplyCategoryFilter();

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

            PaymentPanel.Visibility = payment > 0
       ? Visibility.Visible
       : Visibility.Collapsed;
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


       
        // PRINT
        
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var pd = new PrintDialog();
            if (pd.ShowDialog() != true)
                return;

            // Get printer page size
            double pageWidth = 793.7; 
            double pageHeight = 1122.5;

            // Build a document specifically for printing
            FlowDocument doc = BuildInvoiceDocumentForPrint(pageWidth, pageHeight);

            pd.PrintDocument(
                ((IDocumentPaginatorSource)doc).DocumentPaginator,
                $"Report - {txtBuyerName.Text}"
            );
        }
        private string FormatDecimal(decimal value)
        {
            return value % 1 == 0
                ? ((int)value).ToString()           
                : value.ToString("0.##");           
        }

        private FlowDocument BuildInvoiceDocumentForPrint(double pageWidth, double pageHeight)
        {
            int ROWS_PER_PAGE = 26;   // your requirement

            double spacingSize = 1;
            double marginSize = 0;

            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Arial"),
                FontSize = 15,
                TextAlignment = TextAlignment.Left,
                PageWidth = pageWidth,
                PageHeight = pageHeight,
                PagePadding = new Thickness(marginSize),
                ColumnGap = 0,
                ColumnWidth = pageWidth
            };

            // ========= FETCH DATA (your original logic preserved) =========

            var buyerName = (txtBuyerName.Text ?? string.Empty).Trim().ToUpperInvariant();

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

            // DATES
            var dates = new List<DateTime>();
            foreach (var s in sales)
                if (DateTime.TryParse(s.Date, out var d)) dates.Add(d.Date);

            foreach (var r in returnsList)
                if (DateTime.TryParse(r.Date, out var d)) dates.Add(d.Date);

            dates = dates.Distinct().OrderBy(x => x).ToList();

            var returnTotals = returnsList
                .GroupBy(r => r.ItemName.Trim())
                .ToDictionary(g => g.Key.ToUpperInvariant(), g => g.Sum(x => x.Qty));

            // ITEMS LIST
            var allItemNames = sales.Select(s => s.ItemName)
               .Concat(returnsList.Select(r => r.ItemName))
               .Distinct()
               .OrderBy(name => name)
               .ToList();

            var items = allItemNames.Select(itemName =>
            {
                var salesForItem = sales.Where(s => s.ItemName == itemName);

                decimal unitPrice = salesForItem.Any()
                    ? salesForItem.First().Price
                    : SalesService.GetItemPriceFromMaster(itemName);

                int totalQty = salesForItem.Sum(s => s.Qty);

                int totalReturns = returnTotals.TryGetValue(
                    itemName.Trim().ToUpperInvariant(), out var rt)
                    ? rt : 0;

                int netQty = totalQty - totalReturns;
                decimal netAmount = netQty * unitPrice;

                var dateQty = new Dictionary<DateTime, List<int>>();
                foreach (var s in salesForItem)
                {
                    if (DateTime.TryParse(s.Date, out var sd))
                    {
                        var dk = sd.Date;
                        if (!dateQty.ContainsKey(dk))
                            dateQty[dk] = new List<int>();
                        dateQty[dk].Add(s.Qty);
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
            }).Where(x => x.TotalQty > 0 || x.TotalReturns > 0).ToList();

            int totalColumns = 1 + items.Count;

            // ========= PAGINATION ENGINE =========

            int totalDateRows = dates.Count;
            int pageCount = (int)Math.Ceiling(totalDateRows / (double)ROWS_PER_PAGE);

            int rowIndex = 0;

            for (int page = 1; page <= pageCount; page++)
            {
                
                // ----- PAGE HEADER (ONLY on first page) -----
                if (page == 1)
                    doc.Blocks.Add(BuildBuyerHeaderTable(buyerName, pageWidth));


                // ----- MAIN GRID TABLE -----
                var table = BuildNewPageTable(items, pageWidth);
                doc.Blocks.Add(table);

                var body = new TableRowGroup();
                table.RowGroups.Add(body);

                // ----- ADD 26 rows max -----
                for (int i = 0; i < ROWS_PER_PAGE && rowIndex < totalDateRows; i++, rowIndex++)
                {
                    var date = dates[rowIndex];
                    var row = new TableRow();
                    body.Rows.Add(row);

                    AddExcelCell(row, date.ToString("dd-MMM-yy"), true, false);

                    foreach (var item in items)
                    {
                        string qty = item.DateQty.TryGetValue(date, out var list)
                            ? string.Join("\n", list)
                            : "";

                        AddExcelCell(row, qty, true, false);
                    }
                }

                // ---- If not last page → NO totals, continue loop ----
                if (page != pageCount)
                {
                    doc.Blocks.Add(new Paragraph(new Run("\n"))); // small gap
                    continue;
                }

                // ========= LAST PAGE — ADD TOTALS =========

                bool hasAnyReturns = returnTotals.Any(x => x.Value > 0);

                if (hasAnyReturns)
                {
                    var returnRow = new TableRow();
                    body.Rows.Add(returnRow);
                    AddExcelCell(returnRow, "Return", true, true);

                    foreach (var item in items)
                    {
                        int retQty = returnTotals.TryGetValue(item.Item.Trim().ToUpperInvariant(), out var rq)
                            ? rq : 0;
                        AddExcelCell(returnRow, retQty == 0 ? "" : "-" + rq, true, false);
                    }
                }

                // Qty row
                var qtyRow = new TableRow();
                body.Rows.Add(qtyRow);
                AddExcelCell(qtyRow, "Qty", true, true);
                foreach (var item in items)
                    AddExcelCell(qtyRow, item.NetQty.ToString(), true, false);

                // Price row
                var priceRow = new TableRow();
                body.Rows.Add(priceRow);
                AddExcelCell(priceRow, "Price", true, true);
                foreach (var item in items)
                    AddExcelCell(priceRow, "X " + FormatDecimal(item.UnitPrice), true, false);

                // Total Amount row
                var amountRow = new TableRow();
                body.Rows.Add(amountRow);
                AddExcelCell(amountRow, "Total", true, true);
                foreach (var item in items)
                    AddExcelCell(amountRow, FormatDecimal(item.TotalAmount), true, false);

                // --- Summary rows ---
                decimal grandTotal = items.Sum(x => x.TotalAmount);
                decimal payment = PaymentService.GetPayment(
                    buyerId,
                    _currentMonth.Year,
                    _currentMonth.Month
                );
                decimal balance = grandTotal - payment;

                AddSummaryRow(body, "Grand Total", grandTotal, totalColumns, Brushes.LightCyan, true);
                if (payment > 0)
                {
                    AddSummaryRow(body, "Less Amount", payment, totalColumns, Brushes.LightCyan, true);
                    AddSummaryRow(body, "Due Amount", balance, totalColumns, Brushes.LightCyan, true);
                }
            }

            return doc;
        }
        private Table BuildBuyerHeaderTable(string buyerName, double pageWidth)
        {
            var table = new Table
            {
                CellSpacing = 1,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0)
            };

            table.Columns.Add(new TableColumn { Width = new GridLength(pageWidth) });

            var group = new TableRowGroup();
            table.RowGroups.Add(group);

            var row = new TableRow();
            group.Rows.Add(row);

            var para = new Paragraph(new Run(buyerName))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            };

            var cell = new TableCell(para)
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Background = Brushes.LightCyan,
                Padding = new Thickness(5)
            };

            row.Cells.Add(cell);

            return table;
        }
        private Table BuildNewPageTable(IEnumerable<object> items, double pageWidth)
        {
            double spacingSize = 1;

            var table = new Table
            {
                BorderBrush = Brushes.Black,
                CellSpacing = spacingSize,
                BorderThickness = new Thickness(0),
                Background = Brushes.White,
                Margin = new Thickness(0)
            };

            table.Columns.Clear();

            // Fixed date column width
            const double dateColumnWidth = 60;
            table.Columns.Add(new TableColumn { Width = new GridLength(dateColumnWidth) });

            var itemList = items.ToList();
            int itemCount = itemList.Count;

            if (itemCount > 0)
            {
                double totalGapSpace = (itemCount + 2) * spacingSize;
                double availableWidth = pageWidth - dateColumnWidth - totalGapSpace;
                availableWidth = Math.Max(1, availableWidth);

                double perItemWidth = availableWidth / itemCount;

                for (int i = 0; i < itemCount; i++)
                    table.Columns.Add(new TableColumn { Width = new GridLength(perItemWidth) });
            }

            // Header row
            var headerGroup = new TableRowGroup();
            table.RowGroups.Add(headerGroup);

            var headerRow = new TableRow();
            headerGroup.Rows.Add(headerRow);

            AddExcelCell(headerRow, "Date", true, true);

            foreach (var item in itemList)
            {
                // Use reflection to read "Item" property from anonymous type
                string itemName = (string)item.GetType().GetProperty("Item").GetValue(item);
                AddExcelCell(headerRow, itemName, true, true);
            }

            return table;
        }


        private void AddExcelCell(TableRow row, string text, bool isBold, bool isHeader)
        {
            var p = new Paragraph(new Run(text ?? string.Empty))
            {
                Margin = new Thickness(1,1,1,1),
                TextAlignment = TextAlignment.Center,
                FontSize = 13.5,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal
            };

            var cell = new TableCell(p)
            {
                TextAlignment = TextAlignment.Center,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),  // Increased for double-line effect
                Padding = new Thickness(1, 2, 1, 2)
            };

            if (isHeader)
                cell.Background = Brushes.LightCyan;

            row.Cells.Add(cell);
        }

          private void AddSummaryRow(
    TableRowGroup body,
    string label,
    decimal value,
    int columnSpan,
    Brush background,
    bool bold = true)
        {
            var row = new TableRow();
            body.Rows.Add(row);

            // Label cell
            row.Cells.Add(new TableCell(new Paragraph(new Run(label)))
            {
                TextAlignment = TextAlignment.Center,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(2),
                FontSize = 14,
                Background = background,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = Brushes.Black
            });

            // Value cell
            row.Cells.Add(new TableCell(new Paragraph(new Run($"₹ {value:0,0.00}")))
            {
                ColumnSpan = columnSpan - 1,
                TextAlignment = TextAlignment.Center,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1.5),
                FontSize=17,
                Padding = new Thickness(2),
                Background = background,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = Brushes.Black
            });
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
        private void DgBuyerReport_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.CanUserSort = false;
        }

      

    }
}
