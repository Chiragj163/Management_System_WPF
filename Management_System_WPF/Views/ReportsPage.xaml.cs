using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            var buyers = BuyersService
                .GetAllBuyers()
                .OrderBy(b => b.Name)
                .ToList();

            cmbBuyer.ItemsSource = buyers;
            cmbBuyer.DisplayMemberPath = "Name";
            cmbBuyer.SelectedValuePath = "BuyerId";
        }

        // ================= ALL SALES =================
        private void AllSales_Click(object sender, RoutedEventArgs e)
        {
            if (cmbBuyer.SelectedItem == null)
            {
                MessageBox.Show("Please select a buyer!");
                return;
            }

            var buyer = (Buyer)cmbBuyer.SelectedItem;

            if (!BuyersService.BuyerExists(buyer.BuyerId))
            {
                MessageBox.Show("Selected buyer no longer exists.");
                LoadBuyers();
                return;
            }

            var main = (MainWindow)Application.Current.MainWindow;
            main.ShowFullScreenPage();

            // ✅ CORRECT PAGE
            NavigationService.Navigate(
                new BuyerReportPage(buyer.BuyerId, buyer.Name)
            );
        }


        // ================= SALES BY ARTICLES =================
        private void SalesByArticles_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ArticleReportPage());
        }

        // ================= SALES BY BUYER MATRIX =================
        private void SalesByBuyer_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Application.Current.MainWindow;

            // 🔥 Enable full screen layout (hide side menu, remove margins)
            main.ShowFullScreenPage();

            // Navigate to month-wise Sale By Buyer matrix
            NavigationService.Navigate(new SaleByBuyerPage());
        }

        private void AllSalesReport_Click(object sender, RoutedEventArgs e)
        {
            AllSales_Click(sender, e);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}
