using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;

namespace Management_System_WPF.Views
{
    public partial class BuyerGraphWindow : Window
    {
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }

        public BuyerGraphWindow(Dictionary<string, decimal> data, string title, string xAxisLabel = "Items", bool isQuantity = false)
        
            {
            InitializeComponent();
            txtTitle.Text = title;
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
            DataContext = this;
            SalesChart.AxisX[0].Title = xAxisLabel;
            if (isQuantity)
            {
                // Qty Mode: Plain numbers (e.g., "150")
                SalesChart.AxisY[0].Title = "Quantity";
                Formatter = value => value.ToString("N0");
            }
            else
            {
                // Sales Mode: Currency (e.g., "₹ 150")
                SalesChart.AxisY[0].Title = "Amount (₹)";
                Formatter = value => value.ToString("C0", CultureInfo.CreateSpecificCulture("en-IN"));
            }


            LoadChart(data, isQuantity);
        }

        private void LoadChart(Dictionary<string, decimal> data, bool isQuantity)
        {

            if (data == null || data.Count == 0) return;

            var sortedData = data.OrderByDescending(x => x.Value).ToList();

            Labels = sortedData.Select(x => x.Key).ToArray();

            // ✅ FIX 1: Force the chart to use the new labels immediately
            SalesChart.AxisX[0].Labels = Labels;

            var values = new ChartValues<decimal>(sortedData.Select(x => x.Value));

            SalesChart.Series = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = isQuantity ? "Quantity" : "Sales",
            Values = values,
            DataLabels = true,
            LabelPoint = point => point.Y.ToString("N0"),
            Fill = (Brush)new BrushConverter().ConvertFrom(isQuantity ? "#FF9800" : "#009688"),
            MaxColumnWidth = 50
        }
    };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}