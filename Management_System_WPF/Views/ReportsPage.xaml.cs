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


        private void SalesByArticles_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ArticleReportPage());
        }


        private void SalesByBuyer_Click(object sender, RoutedEventArgs e)
        {
            if (cmbBuyer.SelectedItem == null)
            {
                MessageBox.Show("Please select a buyer!");
                return;
            }

            var buyer = (Buyer)cmbBuyer.SelectedItem;
            MessageBox.Show($"Showing sales for {buyer.Name}. Feature coming soon!");
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
