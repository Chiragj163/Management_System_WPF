using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Management_System_WPF.Views
{
    public partial class SaleByBuyerPage : Page
    {
        private int _year;
        private int _month;

        public SaleByBuyerPage()
        {
            InitializeComponent();

            var today = DateTime.Today;
            _year = today.Year;
            _month = today.Month;

            LoadBuyerMatrix();
        }

        private void LoadBuyerMatrix()
        {
            var data = SalesService.GetBuyerMatrix(_year, _month);

            if (data.Count == 0)
            {
                txtTitle.Text = $"{new DateTime(_year, _month, 1):MMMM yyyy} - No Records";
                dgBuyerSales.ItemsSource = null;
                dgBuyerSales.Columns.Clear();
                return;
            }

            txtTitle.Text = $"{new DateTime(_year, _month, 1):MMMM yyyy}";

            // Collect all unique buyer names from dictionary keys
            var buyers = data
                .SelectMany(r => r.BuyerValues.Keys)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            BuildDynamicColumns(buyers);
            dgBuyerSales.ItemsSource = data;
        }

        private void BuildDynamicColumns(List<string> buyers)
        {
            dgBuyerSales.Columns.Clear();

            // Date column
            dgBuyerSales.Columns.Add(new DataGridTextColumn
            {
                Header = "Date",
                Binding = new Binding("Date") { StringFormat = "dd-MMM-yyyy" },
                Width = 100
            });

            // One column per buyer
            foreach (var b in buyers)
            {
                dgBuyerSales.Columns.Add(new DataGridTextColumn
                {
                    Header = b,
                    Binding = new Binding($"BuyerValues[{b}]")
                    {
                        StringFormat = "₹0.##"
                    },
                    Width = 110
                });
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var dt = new DateTime(_year, _month, 1).AddMonths(-1);
            _year = dt.Year;
            _month = dt.Month;
            LoadBuyerMatrix();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Print feature coming soon.");
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Excel export feature coming soon.");
        }
    }
}
