using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Management_System_WPF.Views
{
    public partial class SalesPage : Page
    {
        private readonly List<CartItem> cart = new();
        private decimal currentItemPrice = 0;

        public SalesPage()
        {
            InitializeComponent();

            cmbBuyer.ItemsSource = BuyersService
                .GetAllBuyers()
                .OrderBy(b => b.Name)
                .ToList();
            cmbBuyer.DisplayMemberPath = "Name";

            cmbItem.ItemsSource = ItemsService.GetAllItems();
            cmbItem.DisplayMemberPath = "Name";

            dpSaleDate.SelectedDate = DateTime.Now;
        }

        // ======================================================
        // LOAD EFFECTIVE PRICE (SPECIAL / NORMAL)
        // ======================================================
        private void LoadEffectivePrice()
        {
            currentItemPrice = 0;

            if (cmbBuyer.SelectedItem is not Buyer buyer)
                return;

            if (cmbItem.SelectedItem is not Item item)
                return;

            try
            {
                currentItemPrice =
                    SpecialPriceService.GetEffectivePrice(buyer.BuyerId, item.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Price Load Error");
                currentItemPrice = item.Price;
            }

            CalculateTotal();
        }

        // ======================================================
        // CALCULATE TOTAL
        // ======================================================
        private void CalculateTotal()
        {
            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0)
            {
                txtTotal.Text = "0.00 ₹";
                return;
            }

            txtTotal.Text = $"{(currentItemPrice * qty):0.00} ₹";
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            LoadEffectivePrice();
        }

        // ======================================================
        // SELECTION EVENTS
        // ======================================================
        private void cmbBuyer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEffectivePrice();
        }

        private void cmbItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEffectivePrice();
            txtQuantity.Focus();
        }

        private void txtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        // ======================================================
        // ADD TO CART
        // ======================================================
        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (cmbBuyer.SelectedItem is not Buyer)
            {
                MessageBox.Show("Please select a buyer.");
                return;
            }

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

            LoadEffectivePrice();

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
                    Price = currentItemPrice
                });
            }

            RefreshCartDisplay();

            // ✅ RESET ONLY ITEM-RELATED FIELDS
            ResetAfterAddToCart();
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

        // ======================================================
        // SAVE SALE
        // ======================================================
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

            DateTime saleDate = dpSaleDate.SelectedDate.Value;

            if (cart.Any())
            {
                decimal total = cart.Sum(c => c.Total);

                int saleId = SalesService.CreateSale(buyer.BuyerId, saleDate, total);

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

            // Direct sale
            if (cmbItem.SelectedItem is not Item item)
                return;

            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0)
                return;

            LoadEffectivePrice();

            decimal amount = currentItemPrice * qty;

            int singleSaleId =
                SalesService.CreateSale(buyer.BuyerId, saleDate, amount);

            SalesService.AddSaleItem(
                singleSaleId,
                item.Id,
                qty,
                currentItemPrice
            );

            MessageBox.Show("Sale saved successfully!");
            ResetForm();
        }

        // ======================================================
        // RESET
        // ======================================================
        private void ResetForm()
        {
            cmbBuyer.SelectedIndex = -1;
            cmbItem.SelectedIndex = -1;

            txtQuantity.Text = "";
            txtTotal.Text = "0.00 ₹";

            dpSaleDate.SelectedDate = DateTime.Today; // ✅ FIX

            cart.Clear();
            RefreshCartDisplay();
        }


        // ======================================================
        // EXIT
        // ======================================================
        private void ExitSalePage_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow main)
                main.MainFrame.Content = null;
        }

        private void dpSaleDate_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 🚫 Do NOT block normal keys
            // Allow Tab, Backspace, Numbers, Numpad, Enter
            if (e.Key == Key.Tab ||
                e.Key == Key.Back ||
                e.Key == Key.Delete ||
                (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                return;
            }

            if (dpSaleDate.SelectedDate == null)
                dpSaleDate.SelectedDate = DateTime.Today;

            DateTime current = dpSaleDate.SelectedDate.Value;

            // Ctrl + Up / Down → Month change
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Up)
                {
                    dpSaleDate.SelectedDate = current.AddMonths(1);
                    e.Handled = true;
                }
                else if (e.Key == Key.Down)
                {
                    dpSaleDate.SelectedDate = current.AddMonths(-1);
                    e.Handled = true;
                }
                return;
            }

            // Up / Down → Day change
            if (e.Key == Key.Up)
            {
                dpSaleDate.SelectedDate = current.AddDays(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                dpSaleDate.SelectedDate = current.AddDays(-1);
                e.Handled = true;
            }
        }
        private void txtQuantity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Calculate_Click(sender, e);
        }
        private void ResetAfterAddToCart()
        {
            cmbItem.SelectedIndex = -1;     // reset item
            txtQuantity.Text = "";          // reset quantity
            txtTotal.Text = "0.00 ₹";       // reset total
            currentItemPrice = 0;           // reset price cache
           // dpSaleDate.SelectedDate = DateTime.Today; // ✅ FIX
            cmbItem.Focus();                // keyboard-friendly
        }


    }
}
