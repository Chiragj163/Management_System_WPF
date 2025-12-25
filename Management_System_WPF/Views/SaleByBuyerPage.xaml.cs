using Management_System_WPF.Helpers;
using Management_System_WPF.Models;
using Management_System_WPF.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Management_System_WPF.Views
{
    public partial class SaleByBuyerPage : Page
    {
        private DateTime _currentMonth;

        public SaleByBuyerPage()
        {
            InitializeComponent();
            ((MainWindow)Application.Current.MainWindow).ShowFullScreenPage();

            // Start from current month (first day)
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            LoadReport(_currentMonth);
        }



        // LOAD REPORT (CURRENT MONTH / PREVIOUS MONTH)

        private void LoadReport(DateTime month)
        {
            var allSales = SalesService.GetSales(); // ALL rows from DB

            // Filter by selected month
            var sales = allSales
                .Where(s => s.SaleDate.Month == month.Month && s.SaleDate.Year == month.Year)
                .ToList();

            if (sales.Count == 0)
            {
                MessageBox.Show("No records found for this month");
                dgBuyerSales.ItemsSource = null;
                dgBuyerSales.Columns.Clear();
                txtTitle.Text = month.ToString("MMMM yyyy");
                UpdateMonthButtons();
                return;
            }

            // Set title “November 2025”
            txtTitle.Text = month.ToString("MMMM yyyy");

            // Collect all unique buyers
            var buyers = sales.Select(s => s.BuyerName).Distinct().ToList();

            // Collect all dates
            var dates = sales.Select(s => s.SaleDate.Date)
                             .Distinct()
                             .OrderBy(d => d)
                             .ToList();

            var rows = new List<SaleByBuyerRow>();

            foreach (var date in dates)
            {
                var row = new SaleByBuyerRow
                {
                    Date = date
                };

                double rowTotal = 0;

                foreach (var buyer in buyers)
                {
                    double total = sales
                        .Where(x => x.BuyerName == buyer && x.SaleDate.Date == date)
                        .Sum(x => x.Qty * x.Price);

                    if (total > 0)
                    {
                        row.BuyerValues[buyer] = total;
                        rowTotal += total;
                    }
                    else
                    {
                        row.BuyerValues[buyer] = null;
                    }
                }

                row.Total = rowTotal == 0 ? null : rowTotal;
                rows.Add(row);
            }
            // ================= TOTAL ROW =================
            var totalRow = new SaleByBuyerRow
            {
                Date = DateTime.MinValue // marker for "Total"
            };

            double grandTotal = 0;

            foreach (var buyer in buyers)
            {
                double colTotal = rows
                    .Where(r => r.BuyerValues.ContainsKey(buyer) && r.BuyerValues[buyer].HasValue)
                    .Sum(r => r.BuyerValues[buyer].Value);

                totalRow.BuyerValues[buyer] = colTotal == 0 ? null : colTotal;
                grandTotal += colTotal;
            }

            totalRow.Total = grandTotal;
            rows.Add(totalRow);



            BuildDynamicColumns(buyers);
            dgBuyerSales.ItemsSource = rows;
            UpdateMonthButtons();

        }


        // BUILD TABLE COLUMNS

        private void BuildDynamicColumns(List<string> buyers)
        {
            dgBuyerSales.Columns.Clear();

            // ================= DATE COLUMN =================
            dgBuyerSales.Columns.Add(new DataGridTextColumn
            {
                Header = "Dates",
                Binding = new Binding("Date")
                {
                    Converter = new DateOrTotalConverter()
                },
                Width = 150,
                IsReadOnly = true
            });

            // ================= BUYER COLUMNS =================
            foreach (var buyer in buyers)
            {
                dgBuyerSales.Columns.Add(new DataGridTextColumn
                {
                    Header = buyer,
                    Binding = new Binding($"BuyerValues[{buyer}]")
                    {
                        StringFormat = "0.##"
                    },
                    Width = 120,
                    IsReadOnly = true
                });
            }

            // ================= TOTAL COLUMN (ONLY ONCE) =================
            dgBuyerSales.Columns.Add(new DataGridTextColumn
            {
                Header = "Total",
                Binding = new Binding("Total")
                {
                    StringFormat = "0.##"
                },
                Width = 140,
                IsReadOnly = true
            });
        }


        public class DateOrTotalConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is DateTime dt && dt == DateTime.MinValue)
                    return "Total";

                return ((DateTime)value).ToString("dd/MM/yyyy");
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
            if (dgBuyerSales.ItemsSource == null)
            {
                MessageBox.Show("Nothing to export!");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Excel CSV (*.csv)|*.csv",
                FileName = "SalesByBuyer_Report.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                using (StreamWriter writer = new StreamWriter(dialog.FileName))
                {
                    // Write header
                    var headers = dgBuyerSales.Columns.Select(c => c.Header.ToString());
                    writer.WriteLine(string.Join(",", headers));

                    // Write all rows
                    foreach (var item in dgBuyerSales.Items)
                    {
                        if (item is SaleByBuyerRow row)
                        {
                            List<string> cells = new List<string>();

                            cells.Add(row.Date.ToString("dd/MM/yyyy"));

                            foreach (var col in dgBuyerSales.Columns.Skip(1))
                            {
                                string buyer = col.Header.ToString();

                                if (row.BuyerValues.ContainsKey(buyer) && row.BuyerValues[buyer].HasValue)
                                    cells.Add(row.BuyerValues[buyer].Value.ToString());
                                else
                                    cells.Add("");
                            }

                            writer.WriteLine(string.Join(",", cells));
                        }
                    }

                }

                MessageBox.Show("Excel file exported successfully!");
            }
        }


        // BUTTON: PRINT

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog print = new PrintDialog();
            if (print.ShowDialog() == true)
            {
                print.PrintVisual(dgBuyerSales, "Sales Report");
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


    }
}
