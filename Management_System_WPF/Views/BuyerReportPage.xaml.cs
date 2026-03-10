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
        private bool _isCustomRange = false;

        private List<SalesRaw> _filteredSales = new();
        private List<SalesRaw> _filteredReturns = new();
        private decimal _currentTotalSales;
        private decimal _currentPayment;
        private List<SalesRaw> _masterSales = new();
        private List<SalesRaw> _masterReturns = new();

      

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
           
            ApplyCategoryFilter();


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
            // 1. Start with the MASTER data (which is already filtered by Date)
            IEnumerable<SalesRaw> salesQuery = _masterSales;
            IEnumerable<SalesRaw> returnsQuery = _masterReturns;

            // 2. Apply Category Logic
            if (cmbCategory.SelectedItem != null)
            {
                string selectedCategory = cmbCategory.SelectedItem.ToString();

                if (selectedCategory != "All")
                {
                    var allItems = ItemsService.GetAllItems()
                                    .ToDictionary(i => i.Name, i => i.Category);

                    salesQuery = salesQuery.Where(s =>
                        allItems.ContainsKey(s.ItemName) &&
                        allItems[s.ItemName] == selectedCategory
                    );

                    returnsQuery = returnsQuery.Where(r =>
                        allItems.ContainsKey(r.ItemName) &&
                        allItems[r.ItemName] == selectedCategory
                    );
                }
            }

            // 3. Update the FILTERED lists (Source for Grid & Print)
            _filteredSales = salesQuery.ToList();
            _filteredReturns = returnsQuery.ToList();

            // 4. Update UI
            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(_filteredSales, _filteredReturns);
            BindPivotToGrid(pivot);

            CalculateMonthlyTotals();
        }

        // =========================================================
        // MAIN LOAD (MONTH MODE)
        // =========================================================
        private void LoadBuyerData()
        {
            _isCustomRange = false;
            if (_isCustomRange)
            {
                _currentPayment = 0;
            }
            ReturnService.Initialize();
            PaymentService.Initialize();

            // 1. Fetch from DB into MASTER
            _masterSales = SalesService.GetSalesRawForPivot(buyerId, _currentMonth.Year, _currentMonth.Month)
                           ?? new List<SalesRaw>();

            _masterReturns = ReturnService.GetReturnsForPivot(buyerId, _currentMonth.Year, _currentMonth.Month)
                             ?? new List<SalesRaw>();

            // 2. Update Date UI
            txtMonthName.Text = _currentMonth.ToString("MMMM yyyy");
            UpdateMonthNavigationButtons();

            // 3. Apply Category Filter (This populates _filtered and updates Grid)
            ApplyCategoryFilter();
        }

        // =========================================================
        // TOTALS
        // =========================================================
        private void CalculateMonthlyTotals()
        {
            // Use FILTERED lists so totals match the grid and print
            var salesList = _filteredSales;
            var returnsList = _filteredReturns;

            // 1. Gross Sales
            decimal grossSales = salesList.Sum(x => x.Qty * x.Price);

            // 2. Return Value
            var allDbItems = ItemsService.GetAllItems();

            decimal totalReturnValue = returnsList.Sum(ret =>
            {
                var sale = salesList.FirstOrDefault(s => s.ItemName == ret.ItemName);
                decimal price = sale?.Price ?? 0;

                if (price == 0)
                {
                    var dbItem = allDbItems.FirstOrDefault(i => i.Name == ret.ItemName);
                    if (dbItem != null) price = dbItem.Price;
                }

                return ret.Qty * price;
            });

            // 3. Net Sales
            _currentTotalSales = grossSales - totalReturnValue;

            // 4. Payment logic
            _currentPayment = PaymentService.GetPayment(
                buyerId,
                _currentMonth.Year,
                _currentMonth.Month
            );

            // If in Custom Date Range mode, hide payment or set to 0
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
            PaymentPanel.Visibility = payment > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // =========================================================
        // RETURN
        // =========================================================
        private void Return_Click(object sender, RoutedEventArgs e)
        {
            // Pass only the context (ID, Year, Month)
            var win = new ReturnWindow(buyerId, _currentMonth.Year, _currentMonth.Month);

            // Refresh main report when window closes
            win.ShowDialog();
            LoadBuyerData();
        }
        // =========================================================
        // PAYMENT
        // =========================================================
      private void Payment_Click(object sender, RoutedEventArgs e)
{
    // 1. Fetch History
    var history = PaymentService.GetPaymentsList(buyerId, _currentMonth.Year, _currentMonth.Month);

    // 2. Define Refresh Action (Simple refresh of the page)
    Action onRefresh = () => 
    {
        LoadBuyerData(); // Recalculate totals
    };

    // 3. Open Window (Pass ID, History, and Refresh Action)
    // Note: We don't pass 'amount' anymore, the window handles inputs internally
    var win = new PaymentWindow(buyerId, history, onRefresh);
    
    win.ShowDialog();
    
    // Refresh one last time when closed to be sure
    LoadBuyerData();
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
                new DateTime(DateTime.Today.Year, 12, 31),
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
            _isCustomRange = true;
            var raw = SalesService.GetSalesBetweenDates(buyerId, from, to)
                      ?? new List<SalesRaw>();

            _masterSales = raw;
            _masterReturns = new List<SalesRaw>(); // Assuming no returns logic for custom range yet

            // 2. Update Date UI
            txtMonthName.Text = label;
            btnPrevMonth.IsEnabled = false;
            btnNextMonth.IsEnabled = false;
            FilterPanel.Visibility = Visibility.Collapsed;

            // 3. Apply Category Filter (This applies the category to the NEW dates)
            ApplyCategoryFilter();
        }
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            // Restore current month
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Reload month data (this also sets master list)
            LoadBuyerData();

            // Reset category to ALL
            cmbCategory.SelectedIndex = 0;

            // Re-enable month navigation
            btnPrevMonth.IsEnabled = true;
            btnNextMonth.IsEnabled = true;
            btnPrevMonth.Opacity = 1.0;
            btnNextMonth.Opacity = 1.0;

            // Hide filter panel
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
            try
            {


                // Get printer page size
                double pageWidth = 780;
                double pageHeight = 1120;

                // Build a document specifically for printing
                FlowDocument doc = BuildInvoiceDocumentForPrint(pageWidth, pageHeight);

                var preview = new PrintPreviewWindow(doc);
                preview.Owner = Window.GetWindow(this); // Center over current app
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating preview: {ex.Message}");
            }

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

            var sales = _filteredSales;
            var returnsList = _filteredReturns;
            if (!sales.Any() && !returnsList.Any())
                {
                    doc.Blocks.Add(new Paragraph(new Run("No data available for printing.")));
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
               
                    doc.Blocks.Add(new Paragraph(new Run("\n"))); // small gap
                   
               
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

                        AddExcelCell(row, date.ToString("dd-MM-yy"), true, false);

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
                    AddExcelCell(qtyRow, "Qty", true, true ,15);
                    foreach (var item in items)
                        AddExcelCell(qtyRow, item.NetQty.ToString(), true, false ,14);

                    // Price row
                    var priceRow = new TableRow();
                    body.Rows.Add(priceRow);
                    AddExcelCell(priceRow, "Price", true, true ,15);
                    foreach (var item in items)
                        AddExcelCell(priceRow, "X " + FormatDecimal(item.UnitPrice), true, false, 14);

                // Total Amount row
                // 1. Calculate dynamic font size
                double baseFontSize = 14;
                double dynamicFontSize = baseFontSize;

                if (items.Count > 14)
                {
                    // Reduce font size by 1 for every item over 14
                    int difference = items.Count - 14;
                    dynamicFontSize = baseFontSize - difference;

                    // ✅ Safety: Never let it go below 11
                    if (dynamicFontSize < 11)
                    {
                        dynamicFontSize = 11;
                    }
                }

                // 2. Create the Row
                var amountRow = new TableRow();
                body.Rows.Add(amountRow);

                // 3. Use the calculated font size
                AddExcelCell(amountRow, "Total", true, true, 15);

                foreach (var item in items)
                {
                    AddExcelCell(amountRow, FormatDecimal(item.TotalAmount), true, false, dynamicFontSize);
                }

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
                        AddSummaryRow(body, "Less", payment, totalColumns, Brushes.LightCyan, true);
                        AddSummaryRow(body, "Due", balance, totalColumns, Brushes.LightCyan, true);
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
                    Padding = new Thickness(0)
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
                const double dateColumnWidth = 59;
                table.Columns.Add(new TableColumn { Width = new GridLength(dateColumnWidth) });

                var itemList = items.ToList();
                int itemCount = itemList.Count;
                if (itemCount > 0)
                {
                   

                    for (int i = 0; i < itemCount; i++)
                    {
                        
                        table.Columns.Add(new TableColumn { Width = GridLength.Auto });
                    }
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


        private void AddExcelCell( TableRow row, string text, bool isBold, bool isHeader, double fontSize = 13.5 )
        {
            var p = new Paragraph(new Run(text ?? string.Empty))
            {
                Margin = new Thickness(1, 1, 1, 1),
                TextAlignment = TextAlignment.Center,
                FontSize = fontSize,
             
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal
            };

            var cell = new TableCell(p)
            {
                TextAlignment = TextAlignment.Center,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0,8,0,8)
            };

            if (isHeader)
                cell.Background = Brushes.LightCyan;

            row.Cells.Add(cell);
        }


        private void AddSummaryRow(  TableRowGroup body, string label, decimal value,  int columnSpan,  Brush background,  bool bold = true)
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
                row.Cells.Add(new TableCell(new Paragraph(new Run($"₹ {value:0,0.##}")))
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

        // Helper function to convert DB data to our PaymentRecord model
        private List<PaymentRecord> GetPaymentHistoryForWindow()
        {
            // Now it fetches real data from DB
            return PaymentService.GetPaymentsList(buyerId, _currentMonth.Year, _currentMonth.Month);
        }
        // 1. Graph for Quantity (Item Name vs Qty)
        private void ViewQtyGraph_Click(object sender, RoutedEventArgs e)
        {
            // ✅ Use _filteredSales so it respects Date AND Category
            var sourceData = _filteredSales;

            if (sourceData == null || !sourceData.Any())
            {
                MessageBox.Show("No data available to graph.");
                return;
            }

            // Group by Item Name and Sum the Quantity
            var graphData = sourceData
                .GroupBy(s => s.ItemName)
                .Select(g => new
                {
                    Item = g.Key,
                    Total = g.Sum(x => x.Qty)
                })
                .Where(x => x.Total > 0) // Exclude items with 0 sales
                .ToDictionary(x => x.Item, x => (decimal)x.Total);

            if (graphData.Count == 0)
            {
                MessageBox.Show("Total quantity is zero.");
                return;
            }

            // Open Graph Window (true = Quantity Mode)
            var graphWin = new BuyerGraphWindow(
                graphData,
                $"Quantity Analysis: {txtMonthName.Text}", // Dynamic Title
                "Items",
                true // isQuantity = true
            );

            graphWin.Owner = Window.GetWindow(this);
            graphWin.ShowDialog();
        }

        // 2. Graph for Amount (Item Name vs Total ₹)
        private void ViewAmountGraph_Click(object sender, RoutedEventArgs e)
        {
            // ✅ Use _filteredSales
            var sourceData = _filteredSales;

            if (sourceData == null || !sourceData.Any())
            {
                MessageBox.Show("No data available to graph.");
                return;
            }

            // Group by Item Name and Sum the Amount (Qty * Price)
            var graphData = sourceData
                .GroupBy(s => s.ItemName)
                .Select(g => new
                {
                    Item = g.Key,
                    Total = g.Sum(x => x.Qty * x.Price)
                })
                .Where(x => x.Total > 0)
                .ToDictionary(x => x.Item, x => x.Total);

            if (graphData.Count == 0)
            {
                MessageBox.Show("Total amount is zero.");
                return;
            }

            // Open Graph Window (false = Amount Mode)
            var graphWin = new BuyerGraphWindow(
                graphData,
                $"Sales Amount: {txtMonthName.Text}",
                "Items",
                false // isQuantity = false
            );

            graphWin.Owner = Window.GetWindow(this);
            graphWin.ShowDialog();
        }
    }
}
