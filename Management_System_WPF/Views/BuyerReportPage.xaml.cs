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
            IEnumerable<SalesRaw> salesQuery = _masterSales;
            IEnumerable<SalesRaw> returnsQuery = _masterReturns;

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

           
            _filteredSales = salesQuery.ToList();
            _filteredReturns = returnsQuery.ToList();

           
            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(_filteredSales, _filteredReturns);
            BindPivotToGrid(pivot);

            CalculateMonthlyTotals();
        }

       
        private void LoadBuyerData()
        {
            _isCustomRange = false;
            if (_isCustomRange)
            {
                _currentPayment = 0;
            }
            ReturnService.Initialize();
            PaymentService.Initialize();

            _masterSales = SalesService.GetSalesRawForPivot(buyerId, _currentMonth.Year, _currentMonth.Month)
                           ?? new List<SalesRaw>();

            _masterReturns = ReturnService.GetReturnsForPivot(buyerId, _currentMonth.Year, _currentMonth.Month)
                             ?? new List<SalesRaw>();

           
            txtMonthName.Text = _currentMonth.ToString("MMMM yyyy");
            UpdateMonthNavigationButtons();

           
            ApplyCategoryFilter();
        }

        
        private void CalculateMonthlyTotals()
        {
           
            var salesList = _filteredSales;
            var returnsList = _filteredReturns;

           
            decimal grossSales = salesList.Sum(x => x.Qty * x.Price);

            
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

           
            _currentTotalSales = grossSales - totalReturnValue;

           
            _currentPayment = PaymentService.GetPayment(
                buyerId,
                _currentMonth.Year,
                _currentMonth.Month
            );

           
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

      
        private void Return_Click(object sender, RoutedEventArgs e)
        {
           
            var win = new ReturnWindow(buyerId, _currentMonth.Year, _currentMonth.Month);

           
            win.ShowDialog();
            LoadBuyerData();
        }
       
        private void Payment_Click(object sender, RoutedEventArgs e)
        {
           
            var history = PaymentService.GetPaymentsList(buyerId, _currentMonth.Year, _currentMonth.Month);

           
            Action onRefresh = () =>
            {
                LoadBuyerData();
            };

          
            var win = new PaymentWindow(buyerId, history, onRefresh);

            win.ShowDialog();

           
            LoadBuyerData();
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
            _masterReturns = new List<SalesRaw>(); 

           
            txtMonthName.Text = label;
            btnPrevMonth.IsEnabled = false;
            btnNextMonth.IsEnabled = false;
            FilterPanel.Visibility = Visibility.Collapsed;

            
            ApplyCategoryFilter();
        }
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
          
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

           
            LoadBuyerData();

          
            cmbCategory.SelectedIndex = 0;

            
            btnPrevMonth.IsEnabled = true;
            btnNextMonth.IsEnabled = true;
            btnPrevMonth.Opacity = 1.0;
            btnNextMonth.Opacity = 1.0;

           
            FilterPanel.Visibility = Visibility.Collapsed;
        }

        
        private void BindPivotToGrid(DataTable pivot)
        {
            dgBuyerReport.Columns.Clear();

            double dateColumnWidth = 150;

            foreach (DataColumn col in pivot.Columns)
            {
                if (col.ColumnName.Equals("Date", StringComparison.OrdinalIgnoreCase))
                {
                    dgBuyerReport.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Date",
                        Binding = new Binding("[Date]"),   
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
                        Binding = new Binding($"[{col.ColumnName}]"),
                        FontSize = 16,
                        IsReadOnly = true,
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                    });
                }
            }

            dgBuyerReport.ItemsSource = pivot.DefaultView;
        }


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



      

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {


               
                double pageWidth = 780;
                double pageHeight = 1120;

               
                FlowDocument doc = BuildInvoiceDocumentForPrint(pageWidth, pageHeight);

                var preview = new PrintPreviewWindow(doc);
                preview.Owner = Window.GetWindow(this);
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
            int ROWS_PER_PAGE = 26; 

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


            var buyerName = (txtBuyerName.Text ?? string.Empty).Trim().ToUpperInvariant();

            var sales = _filteredSales;
            var returnsList = _filteredReturns;
            if (!sales.Any() && !returnsList.Any())
            {
                doc.Blocks.Add(new Paragraph(new Run("No data available for printing.")));
                return doc;
            }


      
            var dates = new List<DateTime>();
            foreach (var s in sales)
                if (DateTime.TryParse(s.Date, out var d)) dates.Add(d.Date);

            foreach (var r in returnsList)
                if (DateTime.TryParse(r.Date, out var d)) dates.Add(d.Date);

            dates = dates.Distinct().OrderBy(x => x).ToList();

            var returnTotals = returnsList
                .GroupBy(r => r.ItemName.Trim())
                .ToDictionary(g => g.Key.ToUpperInvariant(), g => g.Sum(x => x.Qty));

          
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

            int totalDateRows = dates.Count;
            int pageCount = (int)Math.Ceiling(totalDateRows / (double)ROWS_PER_PAGE);

            int rowIndex = 0;

            for (int page = 1; page <= pageCount; page++)
            {

                doc.Blocks.Add(new Paragraph(new Run("\n"))); 

                if (page == 1)
                    doc.Blocks.Add(BuildBuyerHeaderTable(buyerName, pageWidth));


              
                var table = BuildNewPageTable(items, pageWidth);
                doc.Blocks.Add(table);

                var body = new TableRowGroup();
                table.RowGroups.Add(body);

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

                if (page != pageCount)
                {
                    doc.Blocks.Add(new Paragraph(new Run("\n")));
                    continue;
                }


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

                var qtyRow = new TableRow();
                body.Rows.Add(qtyRow);
                AddExcelCell(qtyRow, "Qty", true, true, 15);
                foreach (var item in items)
                    AddExcelCell(qtyRow, item.NetQty.ToString(), true, false, 14);

                var priceRow = new TableRow();
                body.Rows.Add(priceRow);
                AddExcelCell(priceRow, "Price", true, true, 15);
                foreach (var item in items)
                    AddExcelCell(priceRow, "X " + FormatDecimal(item.UnitPrice), true, false, 14);

                double baseFontSize = 14;
                double dynamicFontSize = baseFontSize;

                if (items.Count > 14)
                {
  
                    int difference = items.Count - 14;
                    dynamicFontSize = baseFontSize - difference;

 
                    if (dynamicFontSize < 11)
                    {
                        dynamicFontSize = 11;
                    }
                }


                var amountRow = new TableRow();
                body.Rows.Add(amountRow);

  
                AddExcelCell(amountRow, "Total", true, true, 15);

                foreach (var item in items)
                {
                    AddExcelCell(amountRow, FormatDecimal(item.TotalAmount), true, false, dynamicFontSize);
                }

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
       
            var headerGroup = new TableRowGroup();
            table.RowGroups.Add(headerGroup);

            var headerRow = new TableRow();
            headerGroup.Rows.Add(headerRow);

            AddExcelCell(headerRow, "Date", true, true);

            foreach (var item in itemList)
            {
  
                string itemName = (string)item.GetType().GetProperty("Item").GetValue(item);
                AddExcelCell(headerRow, itemName, true, true);
            }

            return table;
        }


        private void AddExcelCell(TableRow row, string text, bool isBold, bool isHeader, double fontSize = 13.5)
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
                Padding = new Thickness(0, 8, 0, 8)
            };

            if (isHeader)
                cell.Background = Brushes.LightCyan;

            row.Cells.Add(cell);
        }


        private void AddSummaryRow(TableRowGroup body, string label, decimal value, int columnSpan, Brush background, bool bold = true)
        {
            var row = new TableRow();
            body.Rows.Add(row);

           
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

           
            row.Cells.Add(new TableCell(new Paragraph(new Run($"₹ {value:0,0.##}")))
            {
                ColumnSpan = columnSpan - 1,
                TextAlignment = TextAlignment.Center,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1.5),
                FontSize = 17,
                Padding = new Thickness(2),
                Background = background,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = Brushes.Black
            });
        }
 
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

        private List<PaymentRecord> GetPaymentHistoryForWindow()
        {
           
            return PaymentService.GetPaymentsList(buyerId, _currentMonth.Year, _currentMonth.Month);
        }
       
        private void ViewQtyGraph_Click(object sender, RoutedEventArgs e)
        {
           
            var sourceData = _filteredSales;

            if (sourceData == null || !sourceData.Any())
            {
                MessageBox.Show("No data available to graph.");
                return;
            }

           
            var graphData = sourceData
                .GroupBy(s => s.ItemName)
                .Select(g => new
                {
                    Item = g.Key,
                    Total = g.Sum(x => x.Qty)
                })
                .Where(x => x.Total > 0) 
                .ToDictionary(x => x.Item, x => (decimal)x.Total);

            if (graphData.Count == 0)
            {
                MessageBox.Show("Total quantity is zero.");
                return;
            }

          
            var graphWin = new BuyerGraphWindow(
                graphData,
                $"Quantity Analysis: {txtMonthName.Text}",
                "Items",
                true 
            );

            graphWin.Owner = Window.GetWindow(this);
            graphWin.ShowDialog();
        }

      
        private void ViewAmountGraph_Click(object sender, RoutedEventArgs e)
        {
           
            var sourceData = _filteredSales;

            if (sourceData == null || !sourceData.Any())
            {
                MessageBox.Show("No data available to graph.");
                return;
            }

           
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

           
            var graphWin = new BuyerGraphWindow(
                graphData,
                $"Sales Amount: {txtMonthName.Text}",
                "Items",
                false 
            );

            graphWin.Owner = Window.GetWindow(this);
            graphWin.ShowDialog();
        }
    }
}