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
            if (dgBuyerReport.ItemsSource is not DataView dv)
            {
                MessageBox.Show("No data to print.");
                return;
            }

            DataTable table = dv.ToTable();

            string html = DataGridHtmlExporter.ConvertToHtml(
                table,
                txtBuyerName.Text,
                txtMonthName.Text,
                txtTotalSales.Text,
                txtPayment.Text,
                txtBalance.Text
            );

            var preview = new HtmlPreviewWindow(html);
            preview.Owner = Window.GetWindow(this);
            preview.ShowDialog();
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