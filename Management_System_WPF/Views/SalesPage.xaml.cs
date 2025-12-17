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

            cmbBuyer.ItemsSource = BuyersService.GetAllBuyers();
            cmbBuyer.DisplayMemberPath = "Name";

            cmbItem.ItemsSource = ItemsService.GetAllItems();
            cmbItem.DisplayMemberPath = "Name";

            dpSaleDate.SelectedDate = DateTime.Now;
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
                txtTotal.Text = $"{(item.Price * qty):0.00} ₹";
            }
            else
            {
                txtTotal.Text = "0.00 ₹";
            }
        }
        private void Calculate_Click(object sender, RoutedEventArgs e) { CalculateTotal(); }

        // 🟩 ADD TO CART WORKS INDEPENDENTLY
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

            txtTotal.Text = $"{cart.Sum(c => c.Total):0.00} ₹";
        }

        private void RemoveCartItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is CartItem item)
            {
                cart.Remove(item);
                RefreshCartDisplay();
            }
        }

        // 🟩 SAVE SALE WITHOUT CART (DIRECT SALE)
        private void SaveSale_Click(object sender, RoutedEventArgs e)
        {
            if (cmbBuyer.SelectedItem is not Buyer buyer)
            {
                MessageBox.Show("Please select a buyer.");
                return;
            }

            if (dpSaleDate.SelectedDate == null)
            {
                MessageBox.Show("Select sale date.");
                return;
            }

            DateTime selectedDate = dpSaleDate.SelectedDate.Value;

            // 🟢 CASE 1: CART HAS ITEMS
            if (cart.Any())
            {
                decimal totalAmount = cart.Sum(c => c.Total);

                int saleId = SalesService.CreateSale(buyer.BuyerId, selectedDate, totalAmount);

                foreach (var c in cart)
                {
                    SalesService.AddSaleItem(
                        saleId,
                        c.ItemId,
                        c.Quantity,
                        c.Price
                    );
                }

                MessageBox.Show("Sale saved successfully!");
                ResetForm();
                return;
            }

            // 🟢 CASE 2: DIRECT SINGLE ITEM SALE
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

            decimal amount = item.Price * qty;

            int singleSaleId = SalesService.CreateSale(buyer.BuyerId, selectedDate, amount);
            SalesService.AddSaleItem(singleSaleId, item.Id, qty, item.Price);

            MessageBox.Show("Sale saved successfully!");
            ResetForm();
        }


        // 🟩 RESET ENTIRE PAGE
        private void ResetForm()
        {
            cmbBuyer.SelectedIndex = -1;
            cmbItem.SelectedIndex = -1;

            txtQuantity.Text = "";
            txtTotal.Text = "0.00 ₹";

            dpSaleDate.SelectedDate = DateTime.Now;

            cart.Clear();
            RefreshCartDisplay();
        }

        private void ExitSalePage_Click(object sender, RoutedEventArgs e)
        {
            var main = Window.GetWindow(this) as MainWindow;
            main.MainFrame.Content = null;
        }
    }
}
