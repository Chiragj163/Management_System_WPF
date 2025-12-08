using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Management_System_WPF.Views
{
    public partial class SalesPage : Page
    {
        private List<CartItem> cart = new List<CartItem>();

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
                txtTotal.Text = $"{total:0.00} ₹";
            }
            else
            {
                txtTotal.Text = "0 ₹";
            }
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            CalculateTotal();
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (cmbItem.SelectedItem is not Item item)
            {
                MessageBox.Show("Please select an item.");
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Enter valid quantity.");
                return;
            }

            // Check if item already exists
            var existing = cart.FirstOrDefault(c => c.ItemId == item.Id);

            if (existing != null)
            {
                existing.Quantity += qty;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    Quantity = qty,
                    Price = item.Price
                });
            }

            RefreshCartDisplay();
        }

        private void RefreshCartDisplay()
        {
            dgCart.ItemsSource = null;
            dgCart.ItemsSource = cart;

            decimal grandTotal = cart.Sum(c => c.Total);
            txtTotal.Text = $"{grandTotal:0.00} ₹";
        }

        private void RemoveCartItem_Click(object sender, RoutedEventArgs e)
        {
            var cartItem = (sender as Button)?.DataContext as CartItem;

            if (cartItem != null)
            {
                cart.Remove(cartItem);
                RefreshCartDisplay();
            }
        }

        // --- THIS IS THE ONLY SaveSale_Click YOU SHOULD HAVE ---
        private void SaveSale_Click(object sender, RoutedEventArgs e)
        {
            if (cmbBuyer.SelectedItem is not Buyer buyer)
            {
                MessageBox.Show("Please select a buyer.");
                return;
            }

            if (cart.Count == 0)
            {
                MessageBox.Show("Cart is empty.");
                return;
            }

            decimal totalAmount = cart.Sum(i => i.Total);

            int saleId = SalesService.CreateSale(buyer.BuyerId, DateTime.Now, totalAmount);

            foreach (var c in cart)
            {
                SalesService.AddSaleItem(saleId, c.ItemId, c.Quantity, c.Price);
            }

            MessageBox.Show("Sale Saved Successfully!");

            cart.Clear();
            RefreshCartDisplay();
        }


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
