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
        // ======================================================
        // 🔍 LIVE SEARCH FILTERING LOGIC (SAFE & CRASH-FREE)
        // ======================================================
        private void SearchableComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null || cb.ItemsSource == null) return;

            // 1. Ignore navigation keys so the user can use arrows and Enter
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter ||
                e.Key == Key.Tab || e.Key == Key.Escape || e.SystemKey == Key.Down)
            {
                return;
            }

            var textBox = e.OriginalSource as TextBox;
            if (textBox == null) return;

            string searchText = textBox.Text;

            // 2. THE FIX: Use Dispatcher to delay the filter until WPF finishes its key press event!
            // This prevents the application from crashing.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var view = CollectionViewSource.GetDefaultView(cb.ItemsSource);
                if (view == null) return;

                // Apply the filter
                view.Filter = item =>
                {
                    if (string.IsNullOrWhiteSpace(searchText)) return true;

                    string displayValue = GetDisplayValue(item, cb.DisplayMemberPath);
                    return displayValue.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                };

                // Open the dropdown if there is text
                cb.IsDropDownOpen = !string.IsNullOrWhiteSpace(searchText);

                // Keep the cursor at the end of the text so the user can type smoothly
                textBox.CaretIndex = searchText.Length;

            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private string GetDisplayValue(object item, string path)
        {
            if (item == null) return "";
            if (string.IsNullOrEmpty(path)) return item.ToString();
            var prop = item.GetType().GetProperty(path);
            return prop?.GetValue(item)?.ToString() ?? "";
        }

        // ======================================================
        // 💰 LOAD EFFECTIVE PRICE (SPECIAL / NORMAL)
        // ======================================================
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

        // ======================================================
        // 🧮 CALCULATE TOTAL
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
        // 🖱️ SELECTION EVENTS
        // ======================================================
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

        // ======================================================
        // 🛒 CART MANAGEMENT
        // ======================================================
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

        // ======================================================
        // 💾 SAVE SALE
        // ======================================================
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

        // ======================================================
        // 🔄 RESET & EXIT
        // ======================================================
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

        // ======================================================
        // 📅 DATE PICKER KEYBOARD NAVIGATION (TAB SUPPORT)
        // ======================================================
        private void dpSaleDate_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 1. Get the internal TextBox of the DatePicker
            var textBox = dpSaleDate.Template.FindName("PART_TextBox", dpSaleDate) as DatePickerTextBox;
            if (textBox == null) return;

            string text = textBox.Text;

            // --- HANDLE TAB NAVIGATION ---
            if (e.Key == Key.Tab && !string.IsNullOrWhiteSpace(text))
            {
                // Find separators (works for dd/MM/yyyy, MM-dd-yyyy, etc.)
                char[] separators = { '/', '-', '.' };
                int firstSep = text.IndexOfAny(separators);
                int secondSep = firstSep > -1 ? text.IndexOfAny(separators, firstSep + 1) : -1;

                if (firstSep != -1 && secondSep != -1)
                {
                    int selStart = textBox.SelectionStart;
                    int selLen = textBox.SelectionLength;

                    // Define the character positions for Part 1 (Day), Part 2 (Month), Part 3 (Year)
                    int p1Start = 0, p1Len = firstSep;
                    int p2Start = firstSep + 1, p2Len = secondSep - firstSep - 1;
                    int p3Start = secondSep + 1, p3Len = text.Length - secondSep - 1;

                    // SHIFT + TAB (Go Backwards)
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        if (selStart == p3Start && selLen == p3Len) // On Year -> Go to Month
                        {
                            textBox.Select(p2Start, p2Len);
                            e.Handled = true; return;
                        }
                        if (selStart == p2Start && selLen == p2Len) // On Month -> Go to Day
                        {
                            textBox.Select(p1Start, p1Len);
                            e.Handled = true; return;
                        }
                        return; // If on Day, let Shift+Tab exit the control normally
                    }

                    // NORMAL TAB (Go Forwards)
                    if (selLen == text.Length || selLen == 0) // Nothing specific selected -> Select Day
                    {
                        textBox.Select(p1Start, p1Len);
                        e.Handled = true; return;
                    }
                    if (selStart == p1Start && selLen == p1Len) // On Day -> Select Month
                    {
                        textBox.Select(p2Start, p2Len);
                        e.Handled = true; return;
                    }
                    if (selStart == p2Start && selLen == p2Len) // On Month -> Select Year
                    {
                        textBox.Select(p3Start, p3Len);
                        e.Handled = true; return;
                    }

                    // If on Year, let Tab exit the control normally to the next UI element
                    return;
                }
            }

            // --- HANDLE ARROW KEYS (UP/DOWN TO CHANGE DATE) ---
            if (e.Key is Key.Up or Key.Down)
            {
                if (dpSaleDate.SelectedDate == null) dpSaleDate.SelectedDate = DateTime.Today;

                DateTime current = dpSaleDate.SelectedDate.Value;
                int selectionStart = textBox.SelectionStart;

                // Find separators to determine which part the cursor is in
                char[] separators = { '/', '-', '.' };
                int firstSep = text.IndexOfAny(separators);
                int secondSep = firstSep > -1 ? text.IndexOfAny(separators, firstSep + 1) : -1;

                if (firstSep == -1 || secondSep == -1) return;

                int direction = (e.Key == Key.Up) ? 1 : -1;
                DateTime newDate = current;

                // Determine if cursor is in Day, Month, or Year part
                if (selectionStart <= firstSep)
                {
                    // Cursor is in Part 1 (Usually Day)
                    newDate = current.AddDays(direction);
                }
                else if (selectionStart > firstSep && selectionStart <= secondSep)
                {
                    // Cursor is in Part 2 (Usually Month)
                    newDate = current.AddMonths(direction);
                }
                else if (selectionStart > secondSep)
                {
                    // Cursor is in Part 3 (Usually Year)
                    newDate = current.AddYears(direction);
                }

                if (newDate != current)
                {
                    dpSaleDate.SelectedDate = newDate;

                    // Critical: Re-select the text after the UI updates
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Re-find the textbox and restore selection so user can keep pressing Up/Down
                        var tb = dpSaleDate.Template.FindName("PART_TextBox", dpSaleDate) as DatePickerTextBox;
                        tb?.Select(selectionStart, 0);
                        // Note: You can also use selectionLength if you want to keep the part highlighted
                    }), System.Windows.Threading.DispatcherPriority.Background);

                    e.Handled = true;
                }
            }
        }
        private void EnsureSelectedDate()
        {
            if (dpSaleDate.SelectedDate == null)
            {
                dpSaleDate.SelectedDate = DateTime.Today;
            }
        }

        private static bool IsEditingKey(Key key) => key switch
        {
            Key.Back or Key.Delete => true,
            >= Key.D0 and <= Key.D9 => true,
            >= Key.NumPad0 and <= Key.NumPad9 => true,
            Key.OemQuestion or Key.OemQuotes or Key.OemMinus or Key.Divide => true,
            _ => false
        };

        private static bool TryNavigateDatePartWithTab(TextBox textBox)
        {
            string text = textBox.Text;
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            int firstSlash = text.IndexOf('/');
            int secondSlash = text.Length > firstSlash + 1 ? text.IndexOf('/', firstSlash + 1) : -1;

            if (firstSlash == -1 || secondSlash == -1) return false;

            if (selectionStart + selectionLength <= firstSlash)
            {
                textBox.Select(firstSlash + 1, secondSlash - firstSlash - 1);
                return true;
            }
            else if (selectionStart + selectionLength <= secondSlash)
            {
                textBox.Select(secondSlash + 1, text.Length - secondSlash - 1);
                return true;
            }

            return false;
        }
    }
}