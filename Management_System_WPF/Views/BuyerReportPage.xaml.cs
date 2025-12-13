using Management_System_WPF.Helpers;
using Management_System_WPF.Services;
using OfficeOpenXml.Style;
using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Management_System_WPF.Views
{
    public partial class BuyerReportPage : Page
    {
        int buyerId;

        // 🔥 Tracks the MONTH currently being viewed
        private DateTime _currentMonth;

        public BuyerReportPage(int id, string buyerName = "")
        {
            InitializeComponent();

            ((MainWindow)Application.Current.MainWindow).ShowFullScreenPage();

            buyerId = id;

            // Set the buyer name ONCE — do NOT overwrite it later
            if (!string.IsNullOrEmpty(buyerName))
                txtBuyerName.Text = buyerName;

            // Start at current system month
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            LoadBuyerData();
        }

        // =====================================================
        // 🔵 LOAD DATA FOR SELECTED MONTH
        // =====================================================
        private void LoadBuyerData()
        {
            var raw = SalesService.GetSalesRawForPivot(
                buyerId,
                _currentMonth.Year,
                _currentMonth.Month
            );

            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(raw);
            BindPivotToGrid(pivot);

            double total = raw.Sum(r => (double)(r.Price * r.Qty));
            txtGrandTotal.Text = total.ToString("₹0.00");

            // 🔥 SHOW MONTH NAME (not buyer name)
            txtMonthName.Text = _currentMonth.ToString("MMMM yyyy");

            // 🔥 UPDATE BUTTON STATES
            UpdateMonthNavigationButtons();
        }

        // =====================================================
        // 🔵 PREVIOUS MONTH BUTTON
        // =====================================================
        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var prev = SalesService.GetPreviousSalesMonthForBuyer(buyerId, _currentMonth);

            if (prev != null)
            {
                _currentMonth = prev.Value;
                LoadBuyerData();
            }
        }


        // =====================================================
        // 🔵 NEXT MONTH BUTTON
        // =====================================================
        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            var next = SalesService.GetNextSalesMonthForBuyer(buyerId, _currentMonth);

            if (next != null)
            {
                _currentMonth = next.Value;
                LoadBuyerData();
            }
        }


        // =====================================================
        // 🔵 ENABLE/DISABLE NAVIGATION BUTTONS
        // =====================================================
        private void UpdateMonthNavigationButtons()
        {
            btnPrevMonth.IsEnabled =
                SalesService.GetPreviousSalesMonthForBuyer(buyerId, _currentMonth) != null;

            btnNextMonth.IsEnabled =
                SalesService.GetNextSalesMonthForBuyer(buyerId, _currentMonth) != null;

            btnPrevMonth.Opacity = btnPrevMonth.IsEnabled ? 1.0 : 0.4;
            btnNextMonth.Opacity = btnNextMonth.IsEnabled ? 1.0 : 0.4;
        }


        // =====================================================
        // 🔵 BUILD GRID
        // =====================================================
        private void BindPivotToGrid(DataTable pivot)
        {
            dgBuyerReport.Columns.Clear();

            foreach (DataColumn col in pivot.Columns)
            {
                dgBuyerReport.Columns.Add(new DataGridTextColumn
                {
                    Header = col.ColumnName,
                    Binding = new Binding(col.ColumnName),
                    FontSize = 16,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });
            }

            dgBuyerReport.ItemsSource = pivot.DefaultView;
        }

        // =====================================================
        // 🔵 BACK & PRINT
        // =====================================================
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Application.Current.MainWindow;
            main.ResetLayoutBeforeNavigation();
            NavigationService.GoBack();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
                pd.PrintVisual(this.Content as Visual, "Buyer Report Print");
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
            DateTime from = DateTime.Now.AddMonths(-6);
            DateTime to = DateTime.Now;

            var raw = SalesService.GetSalesBetweenDates(buyerId, from, to);

            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(raw);
            BindPivotToGrid(pivot);

            txtMonthName.Text = "Last 6 Months";
            txtGrandTotal.Text = raw.Sum(r => (double)(r.Price * r.Qty)).ToString("₹0.00");

            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterYear_Click(object sender, RoutedEventArgs e)
        {
            DateTime from = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime to = DateTime.Now;

            var raw = SalesService.GetSalesBetweenDates(buyerId, from, to);

            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(raw);
            BindPivotToGrid(pivot);

            txtMonthName.Text = DateTime.Now.Year + " (Year)";
            txtGrandTotal.Text = raw.Sum(r => (double)(r.Price * r.Qty)).ToString("₹0.00");

            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterCustom_Click(object sender, RoutedEventArgs e)
        {
            if (dpFrom.SelectedDate == null || dpTo.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates.");
                return;
            }

            DateTime from = dpFrom.SelectedDate.Value;
            DateTime to = dpTo.SelectedDate.Value;

            var raw = SalesService.GetSalesBetweenDates(buyerId, from, to);

            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(raw);
            BindPivotToGrid(pivot);

            txtMonthName.Text = $"{from:dd MMM yyyy} → {to:dd MMM yyyy}";
            txtGrandTotal.Text = raw.Sum(r => (double)(r.Price * r.Qty)).ToString("₹0.00");

            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataView dv = dgBuyerReport.ItemsSource as DataView;
                if (dv == null || dv.Count == 0)
                {
                    MessageBox.Show("No data available to export.");
                    return;
                }

                // Convert DataView → DataTable
                DataTable dt = dv.ToTable();

                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Buyer Report");

                    // 🔥 Write Headers
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        ws.Cells[1, col + 1].Value = dt.Columns[col].ColumnName;
                        ws.Cells[1, col + 1].Style.Font.Bold = true;
                        ws.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Teal);
                        ws.Cells[1, col + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                        ws.Column(col + 1).Width = 18;
                    }

                    // 🔥 Write Data Rows
                    for (int row = 0; row < dt.Rows.Count; row++)
                    {
                        for (int col = 0; col < dt.Columns.Count; col++)
                        {
                            ws.Cells[row + 2, col + 1].Value = dt.Rows[row][col];
                        }
                    }

                    // 🔥 Apply Borders
                    var range = ws.Cells[1, 1, dt.Rows.Count + 1, dt.Columns.Count];
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    // 🔥 AutoFit
                    ws.Cells.AutoFitColumns();

                    // 🔥 Create file
                    string fileName = $"BuyerReport_{txtBuyerName.Text}_{DateTime.Now:yyyyMMdd}.xlsx";
                    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                    File.WriteAllBytes(filePath, package.GetAsByteArray());

                    MessageBox.Show($"Excel Exported Successfully!\nSaved at Desktop:\n{fileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Excel Export Failed:\n" + ex.Message);
            }
        }


    }
}
