using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Management_System_WPF.Views
{
    public partial class BuyersPage : Page
    {
        public BuyersPage()
        {
            InitializeComponent();
            LoadBuyers();
        }

        private void LoadBuyers()
        {
            var buyers = BuyersService.GetAllBuyers();

            // Add serial number dynamically
            int count = 1;
            foreach (var b in buyers)
            {
                b.SerialNumber = count++;
            }

            dgBuyers.ItemsSource = buyers;
        }

        private void ExitBuyerPage_Click(object sender, RoutedEventArgs e)
        {
            // Go back to previous page
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                // Or clear the frame (go home)
                ((MainWindow)Application.Current.MainWindow).MainFrame.Content = null;
            }
        }


        private void SaveBuyer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuyerName.Text))
            {
                MessageBox.Show("Please enter buyer name.");
                return;
            }

            var buyer = new Buyer { Name = txtBuyerName.Text };
            BuyersService.AddBuyer(buyer);

            txtBuyerName.Text = "";

            LoadBuyers();

            // FIX: Avoid double-click problem
            Keyboard.ClearFocus();
        }

    }
}
