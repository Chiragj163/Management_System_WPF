using System.Windows;
using System.Windows.Controls;
using Management_System_WPF.Services;
using Management_System_WPF.Models;

namespace Management_System_WPF.Views
{
    public partial class ReportsPage : Page
    {
        public ReportsPage()
        {
            InitializeComponent();
            LoadBuyers();
        }

        private void LoadBuyers()
        {
            var buyers = BuyersService.GetAllBuyers();
            cmbBuyer.ItemsSource = buyers;
            cmbBuyer.DisplayMemberPath = "Name";
            cmbBuyer.SelectedValuePath = "BuyerId";
        }

        // ===========================
        // 1️⃣ ALL SALES → SELECT BUYER REQUIRED
        // ===========================
        private void AllSales_Click(object sender, RoutedEventArgs e)
        {
            if (cmbBuyer.SelectedItem == null)
            {
                MessageBox.Show("Please select a buyer!");
                return;
            }

            var buyer = (Buyer)cmbBuyer.SelectedItem;

            NavigationService.Navigate(new BuyerReportPage(buyer.BuyerId, buyer.Name));
        }

        // ===========================
        // 2️⃣ SALES BY ARTICLES → NO BUYER NEEDED
        // ===========================
        private void SalesByArticles_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ArticleReportPage());
        }

        // ===========================
        // 3️⃣ SALE BY BUYER (MATRIX REPORT) → NO BUYER SELECTION REQUIRED
        // ===========================
        private void SalesByBuyer_Click(object sender, RoutedEventArgs e)
        {
            // ❌ Remove buyer selection requirement
            // ✔️ Load matrix report for ALL buyers
            NavigationService.Navigate(new SaleByBuyerPage());
        }

        private void AllSalesReport_Click(object sender, RoutedEventArgs e)
        {
            AllSales_Click(sender, e);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
