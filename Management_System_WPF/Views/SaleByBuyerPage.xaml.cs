using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;

namespace Management_System_WPF.Views
{
    public partial class SaleByBuyerPage : Page
    {
        public SaleByBuyerPage()
        {
            InitializeComponent();
            LoadReport(DateTime.Now);
        }

        // ==========================================
        // LOAD REPORT (CURRENT MONTH / PREVIOUS MONTH)
        // ==========================================
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
                var row = new SaleByBuyerRow();
                row.Date = date;

                foreach (var buyer in buyers)
                {
                    double total = sales
                        .Where(x => x.BuyerName == buyer && x.SaleDate.Date == date)
                        .Sum(x => x.TotalAmount);

                    row.BuyerValues[buyer] = total;
                }

                rows.Add(row);
            }

            BuildDynamicColumns(buyers);
            dgBuyerSales.ItemsSource = rows;
        }

        // ==========================================
        // BUILD TABLE COLUMNS
        // ==========================================
        private void BuildDynamicColumns(List<string> buyers)
        {
            dgBuyerSales.Columns.Clear();

            // DATE column
            dgBuyerSales.Columns.Add(new DataGridTextColumn
            {
                Header = "Dates",
                Binding = new System.Windows.Data.Binding("Date") { StringFormat = "dd-MMM-yyyy" },
                Width = 150
            });

            // Dynamic Buyer Columns
            foreach (var buyer in buyers)
            {
                dgBuyerSales.Columns.Add(new DataGridTextColumn
                {
                    Header = buyer,
                    Binding = new System.Windows.Data.Binding($"BuyerValues[{buyer}]"),
                    Width = 150
                });
            }
        }

        // ==========================================
        // BUTTON: PREVIOUS MONTH
        // ==========================================
        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            DateTime previous = DateTime.Now.AddMonths(-1);
            LoadReport(previous);
        }

        // ==========================================
        // BUTTON: EXPORT TO EXCEL (CSV format)
        // ==========================================
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

                            cells.Add(row.Date.ToString("dd-MMM-yyyy"));

                            foreach (var col in dgBuyerSales.Columns.Skip(1))
                            {
                                string buyer = col.Header.ToString();
                                double val = row.BuyerValues.ContainsKey(buyer)
                                    ? row.BuyerValues[buyer]
                                    : 0;

                                cells.Add(val.ToString());
                            }

                            writer.WriteLine(string.Join(",", cells));
                        }
                    }
                }

                MessageBox.Show("Excel file exported successfully!");
            }
        }

        // ==========================================
        // BUTTON: PRINT
        // ==========================================
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
            NavigationService?.GoBack();
        }
    }
}
