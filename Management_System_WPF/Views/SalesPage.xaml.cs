using Management_System_WPF.Helpers;
using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace Management_System_WPF.Views
{
    public partial class SalesPage : Page
    {
        private readonly List<CartItem> cart = new();
        private decimal currentItemPrice = 0;
        private char _lastCharPressed = '\0';
        private DateTime _lastCharTime = DateTime.MinValue;
        private int _charCycleIndex = -1;
        private const int CycleTimeoutSeconds = 5;
        public SalesPage()
        {
            InitializeComponent();
           
            // Load Buyers
            cmbBuyer.ItemsSource = BuyersService
                .GetAllBuyers()
                .OrderBy(b => b.Name)
                .ToList();
            cmbBuyer.DisplayMemberPath = "Name";

            // Load Items
            cmbItem.ItemsSource = ItemsService.GetAllItems().OrderBy(a => a.Name).ToList();
            cmbItem.DisplayMemberPath = "Name";

            dpSaleDate.SelectedDate = DateTime.Now;
        }
        private List<object> GetItemsStartingWith(ComboBox cb, char letter)
        {
            var items = new List<object>();

            foreach (var item in cb.ItemsSource.Cast<object>())
            {
                string value = GetDisplayValue(item, cb.DisplayMemberPath);

                if (!string.IsNullOrEmpty(value) &&
                    value.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(item);
                }
            }

            return items;
        }
        private void btnSaveSale_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                e.Handled = true; 
                FocusBuyer();     
            }
        }
        private void FocusBuyer()
        {
            cmbBuyer.Focus();
            var textBox = cmbBuyer.Template.FindName("PART_EditableTextBox", cmbBuyer) as TextBox;
            if (textBox != null)
            {
                textBox.Focus();
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
        private void SearchableComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (sender == cmbBuyer)
                {
                    var saveBtn = this.FindName("btnSaveSale") as Button;
                    saveBtn?.Focus();
                    e.Handled = true;
                    return;
                }
            }
            var cb = sender as ComboBox;
            if (cb == null || cb.ItemsSource == null) return;

            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter ||
                e.Key == Key.Tab || e.Key == Key.Escape)
                return;

            var textBox = e.OriginalSource as TextBox;
            if (textBox == null) return;

            string searchText = textBox.Text;

            // BACKSPACE resets cycling
            if (e.Key == Key.Back)
            {
                _lastCharPressed = '\0';
                _charCycleIndex = -1;
            }

            // -------- ALPHABET CYCLING --------
            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                char pressedChar = (char)('A' + (e.Key - Key.A));
                DateTime now = DateTime.Now;

                bool sameKey = _lastCharPressed == pressedChar;
                bool withinTime = (now - _lastCharTime).TotalSeconds <= CycleTimeoutSeconds;

                // Only cycle if SAME key pressed again within time
                if (sameKey && withinTime)
                {
                    var matches = GetItemsStartingWith(cb, pressedChar);

                    if (matches.Count > 0)
                    {
                        _charCycleIndex++;

                        if (_charCycleIndex >= matches.Count)
                            _charCycleIndex = 0;

                        cb.SelectedItem = matches[_charCycleIndex];

                        cb.UpdateLayout();

                        var container = cb.ItemContainerGenerator
                            .ContainerFromItem(matches[_charCycleIndex]) as ComboBoxItem;

                        container?.BringIntoView();

                        textBox.SelectAll();
                    }

                    _lastCharTime = now;
                    return;
                }

                // first press
                _lastCharPressed = pressedChar;
                _lastCharTime = now;
                _charCycleIndex = -1;
            }

            // -------- NORMAL SEARCH FILTER --------
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var view = CollectionViewSource.GetDefaultView(cb.ItemsSource);
                if (view == null) return;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    view.Filter = null;
                    cb.SelectedIndex = -1;
                    cb.IsDropDownOpen = false;

                    _lastCharPressed = '\0';
                    _charCycleIndex = -1;
                    return;
                }

                view.Filter = item =>
                {
                    string displayValue = GetDisplayValue(item, cb.DisplayMemberPath);
                    return displayValue.IndexOf(searchText,
                        StringComparison.OrdinalIgnoreCase) >= 0;
                };

                cb.IsDropDownOpen = true;
                textBox.CaretIndex = textBox.Text.Length;

            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private string GetDisplayValue(object item, string path)
        {
            if (item == null) return "";
            if (string.IsNullOrEmpty(path)) return item.ToString();
            var prop = item.GetType().GetProperty(path);
            return prop?.GetValue(item)?.ToString() ?? "";
        }

        private void LoadEffectivePrice()
        {
            currentItemPrice = 0;

            if (cmbBuyer.SelectedItem is not Buyer buyer) return;
            if (cmbItem.SelectedItem is not Item item) return;

            try
            {
                currentItemPrice = SpecialPriceService.GetEffectivePrice(buyer.BuyerId, item.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Price Load Error");
                currentItemPrice = item.Price;
            }

            CalculateTotal();
        }

      
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

      
        private void cmbBuyer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEffectivePrice();
        }

        private void cmbItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEffectivePrice();
        }

        private void txtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void txtQuantity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Calculate_Click(sender, e);
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            TryAddCurrentSelectionToCart();
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

        private bool TryAddCurrentSelectionToCart()
        {
            if (cmbBuyer.SelectedItem is not Buyer buyer)
            {
                MessageBox.Show("Please select a buyer.");
                return false;
            }

            if (cmbItem.SelectedItem is not Item item)
            {
                MessageBox.Show("Please select an item.");
                return false;
            }

            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Enter valid quantity.");
                return false;
            }

            if (dpSaleDate.SelectedDate == null)
            {
                MessageBox.Show("Select sale date.");
                return false;
            }

            LoadEffectivePrice();
            DateTime saleDate = dpSaleDate.SelectedDate.Value.Date;

            var existing = cart.FirstOrDefault(c =>
                c.BuyerId == buyer.BuyerId &&
                c.ItemId == item.Id &&
                c.SaleDate == saleDate
            );

            if (existing != null)
            {
                existing.Quantity += qty;
            }
            else
            {
                cart.Add(new CartItem
                {
                    BuyerId = buyer.BuyerId,
                    BuyerName = buyer.Name,
                    ItemId = item.Id,
                    ItemName = item.Name,
                    Quantity = qty,
                    Price = currentItemPrice,
                    SaleDate = saleDate
                });
            }

            RefreshCartDisplay();
            ResetAfterAddToCart();
            return true;
        }

        private void AutoAddPendingSelectionIfAny()
        {
            if (cmbBuyer.SelectedItem is not Buyer buyer) return;
            if (cmbItem.SelectedItem is not Item item) return;
            if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0) return;
            if (dpSaleDate.SelectedDate == null) return;

            DateTime saleDate = dpSaleDate.SelectedDate.Value.Date;

            bool exists = cart.Any(c =>
                c.BuyerId == buyer.BuyerId &&
                c.ItemId == item.Id &&
                c.SaleDate == saleDate
            );

            if (!exists)
            {
                TryAddCurrentSelectionToCart();
            }
        }

        private void SaveSale_Click(object sender, RoutedEventArgs e)
        {
            AutoAddPendingSelectionIfAny();

            if (!cart.Any())
            {
                MessageBox.Show("Nothing to save.");
                return;
            }
            try
            {
                foreach (var item in cart)
                {
                    int saleId = SalesService.CreateSale(item.BuyerId, item.SaleDate, item.Total);
                    SalesService.AddSaleItem(saleId, item.ItemId, item.Quantity, item.Price);
                }

                MessageBox.Show("Sales saved successfully!");
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving sales: {ex.Message}");
            }
        }

        private void ResetForm()
        {
            cmbBuyer.SelectedIndex = -1;
            cmbItem.SelectedIndex = -1;

            txtQuantity.Text = "";
            txtTotal.Text = "0.00 ₹";

            dpSaleDate.SelectedDate = DateTime.Today;

            cart.Clear();
            RefreshCartDisplay();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                cmbBuyer.Focus();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ResetAfterAddToCart()
        {
            cmbItem.SelectedIndex = -1;
            txtQuantity.Text = "";
            txtTotal.Text = "0.00 ₹";
            currentItemPrice = 0;
            cmbItem.Focus();
        }

        private void ExitSalePage_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow main)
                main.MainFrame.Content = null;
        }

        private void dpSaleDate_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            
            var textBox = dpSaleDate.Template.FindName("PART_TextBox", dpSaleDate) as DatePickerTextBox;
            if (textBox == null) return;

            string text = textBox.Text;

           
            if (e.Key == Key.Tab && !string.IsNullOrWhiteSpace(text))
            {
              
                char[] separators = { '/', '-', '.' };
                int firstSep = text.IndexOfAny(separators);
                int secondSep = firstSep > -1 ? text.IndexOfAny(separators, firstSep + 1) : -1;

                if (firstSep != -1 && secondSep != -1)
                {
                    int selStart = textBox.SelectionStart;
                    int selLen = textBox.SelectionLength;
                    int p1Start = 0, p1Len = firstSep;
                    int p2Start = firstSep + 1, p2Len = secondSep - firstSep - 1;
                    int p3Start = secondSep + 1, p3Len = text.Length - secondSep - 1;

                  
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        if (selStart == p3Start && selLen == p3Len) 
                        {
                            textBox.Select(p2Start, p2Len);
                            e.Handled = true; return;
                        }
                        if (selStart == p2Start && selLen == p2Len) 
                        {
                            textBox.Select(p1Start, p1Len);
                            e.Handled = true; return;
                        }
                        return; 
                    }

                    
                    if (selLen == text.Length || selLen == 0)
                    {
                        textBox.Select(p1Start, p1Len);
                        e.Handled = true; return;
                    }
                    if (selStart == p1Start && selLen == p1Len) 
                    {
                        textBox.Select(p2Start, p2Len);
                        e.Handled = true; return;
                    }
                    if (selStart == p2Start && selLen == p2Len) 
                    {
                        textBox.Select(p3Start, p3Len);
                        e.Handled = true; return;
                    }

                   
                    return;
                }
            }

            if (e.Key is Key.Up or Key.Down)
            {
                if (dpSaleDate.SelectedDate == null) dpSaleDate.SelectedDate = DateTime.Today;

                DateTime current = dpSaleDate.SelectedDate.Value;
                int selectionStart = textBox.SelectionStart;

                char[] separators = { '/', '-', '.' };
                int firstSep = text.IndexOfAny(separators);
                int secondSep = firstSep > -1 ? text.IndexOfAny(separators, firstSep + 1) : -1;

                if (firstSep == -1 || secondSep == -1) return;

                int direction = (e.Key == Key.Up) ? 1 : -1;
                DateTime newDate = current;

                // Variables to store the part we want to re-select after the update
                int targetStart, targetLen;

                if (selectionStart <= firstSep)
                {
                    newDate = current.AddDays(direction);
                    targetStart = 0;
                    targetLen = firstSep;
                }
                else if (selectionStart > firstSep && selectionStart <= secondSep)
                {
                    newDate = current.AddMonths(direction);
                    targetStart = firstSep + 1;
                    targetLen = secondSep - firstSep - 1;
                }
                else
                {
                    newDate = current.AddYears(direction);
                    targetStart = secondSep + 1;
                    targetLen = text.Length - secondSep - 1;
                }

                if (newDate != current)
                {
                    dpSaleDate.SelectedDate = newDate;

                    // Use Background priority to ensure the text has updated before we select it
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var tb = dpSaleDate.Template.FindName("PART_TextBox", dpSaleDate) as DatePickerTextBox;
                        if (tb != null)
                        {
                            // 🔥 THE FIX: Instead of just putting the cursor, 
                            // we highlight the whole part. Now Tab will know where we are.
                            tb.Select(targetStart, targetLen);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);

                    e.Handled = true;
                }
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Use Dispatcher to ensure the UI has finished rendering before focusing
            Dispatcher.BeginInvoke(new Action(() =>
            {
                cmbBuyer.Focus();

                // Target the internal TextBox of the Editable ComboBox
                var textBox = cmbBuyer.Template.FindName("PART_EditableTextBox", cmbBuyer) as TextBox;
                if (textBox != null)
                {
                    textBox.Focus();
                    textBox.CaretIndex = textBox.Text.Length; // Put cursor at the end
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
        private void ComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null) return;

            if (e.Key == Key.Enter)
            {
                if (cb.IsDropDownOpen)
                {
                    // 🔥 Select current highlighted item
                    if (cb.SelectedItem == null && cb.Items.Count > 0)
                    {
                        cb.SelectedIndex = 0;
                    }

                    cb.IsDropDownOpen = false;

                    // Move focus next (optional)
                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    cb.MoveFocus(request);

                    e.Handled = true;
                }
            }
        }
    }
}