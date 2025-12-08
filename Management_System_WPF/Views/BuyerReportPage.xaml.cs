using Management_System_WPF.Services;
using System.Windows;
using System.Windows.Controls;

namespace Management_System_WPF.Views
{
    public partial class BuyerReportPage : Page
    {
        int buyerId;

        public BuyerReportPage(int id, string buyerName = "")
        {
            InitializeComponent();

            buyerId = id;

            if (!string.IsNullOrEmpty(buyerName))
                txtBuyerName.Text = $"Sale History of {buyerName}";

            LoadBuyerData();
        }

        private void LoadBuyerData()
        {
            var records = SalesService.GetSalesByBuyer(buyerId);
            dgBuyerReport.ItemsSource = records;

            double total = 0;
            foreach (var r in records)
                total += r.Total;

            txtGrandTotal.Text = total.ToString("₹0.00");
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var records = SalesService.GetPreviousMonthSales(buyerId);
            dgBuyerReport.ItemsSource = records;

            double total = 0;
            foreach (var r in records)
                total += r.Total;

            txtGrandTotal.Text = total.ToString("₹0.00");

            MessageBox.Show($"Loaded {records.Count} previous month records");
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                pd.PrintVisual(MainContainer, "Buyer Report Print");
            }
        }
    }
}
