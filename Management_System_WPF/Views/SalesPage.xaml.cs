using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Management_System_WPF.Views
{
    public partial class SalesPage : Page
    {
        public SalesPage()
        {
            InitializeComponent();

            // Load Buyers
            cmbBuyer.ItemsSource = BuyersService.GetAllBuyers();
            cmbBuyer.DisplayMemberPath = "Name";

            // Load Items
            cmbItem.ItemsSource = ItemsService.GetAllItems();
            cmbItem.DisplayMemberPath = "Name";
        }

        private void txtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            if (cmbItem.SelectedItem is Item item &&
                int.TryParse(txtQuantity.Text, out int qty))
            {
                decimal total = item.Price * qty;
                txtTotal.Text = total.ToString("0.00") + " ₹";
            }
            else
            {
                txtTotal.Text = "0 ₹";
            }
        }

        private void SaveSale_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sale Saved Successfully!");
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            CalculateTotal();
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (cmbBuyer.SelectedItem == null)
            {
                MessageBox.Show("Please select buyer");
                return;
            }
            if (cmbItem.SelectedItem == null)
            {
                MessageBox.Show("Please select item");
                return;
            }
            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Please enter valid quantity");
                return;
            }

            MessageBox.Show("Item added to cart (functionality coming soon)");
        }

        // FIXED EXIT BUTTON
        private void ExitSalePage_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainFrame.Content = null;
            }
        }
    }
}
