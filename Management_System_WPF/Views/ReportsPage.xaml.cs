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


        // 1️⃣ ALL SALES → OPEN BuyerReportPage IN FULL SCREEN
        private void AllSales_Click(object sender, RoutedEventArgs e)
        {
            if (cmbBuyer.SelectedItem == null)
            {
                MessageBox.Show("Please select a buyer!");
                return;
            }

            var buyer = (Buyer)cmbBuyer.SelectedItem;

            // 🔥 Make layout fullscreen (hide side menu, remove margin)
            var main = (MainWindow)Application.Current.MainWindow;
            main.ShowFullScreenPage();

            // Navigate to BuyerReportPage
            NavigationService.Navigate(new BuyerReportPage(buyer.BuyerId, buyer.Name));
        }

        // 2️⃣ SALES BY ARTICLES – (decide if you want fullscreen here or not)
        private void SalesByArticles_Click(object sender, RoutedEventArgs e)
        {
            // If you want ArticleReport full-screen too, uncomment:
            // ((MainWindow)Application.Current.MainWindow).ShowFullScreenPage();

            NavigationService.Navigate(new ArticleReportPage());
        }

        // 3️⃣ SALES BY BUYER (matrix) – same note as above
        private void SalesByBuyer_Click(object sender, RoutedEventArgs e)
        {
            // If you want matrix report full-screen:
            // ((MainWindow)Application.Current.MainWindow).ShowFullScreenPage();

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
