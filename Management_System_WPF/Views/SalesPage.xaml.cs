using Management_System_WPF.Helpers;
using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

            cmbItem.ItemsSource = ItemsService.GetAllItems().OrderBy(a=>a.Name).ToList();
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
            Dispatcher.BeginInvoke(new Action(() =>
            {
                cmbBuyer.Focus();
               // cmbBuyer.IsDropDownOpen = true;
                
            }), System.Windows.Threading.DispatcherPriority.Background);

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
            // Ensure we're working with the DatePicker's internal TextBox (where editing happens)
            if (dpSaleDate.Template.FindName("PART_TextBox", dpSaleDate) is not DatePickerTextBox dateTextBox)
                return;

            // The DatePickerTextBox does not have a 'Child' property.
            // Instead, just use 'dateTextBox' directly as a TextBox.
            var textBox = dateTextBox as TextBox;

            if (textBox == null) return;

            // 1. Handle Tab key for navigating between Day → Month → Year
            if (e.Key == Key.Tab)
            {
                if (TryNavigateDatePartWithTab(textBox))
                {
                    e.Handled = true;
                }
                return;
            }

            // 2. Allow standard editing keys (digits, backspace, delete, slash, etc.)
            if (IsEditingKey(e.Key))
            {
                return; // Let the TextBox handle it naturally
            }

            // 3. Handle arrow keys for date adjustment
            if (e.Key is Key.Up or Key.Down)
            {
                EnsureSelectedDate();

                var current = dpSaleDate.SelectedDate.Value;

                DateTime newDate = (e.Key, Keyboard.Modifiers) switch
                {
                    (Key.Up, ModifierKeys.Control) => current.AddMonths(1),
                    (Key.Down, ModifierKeys.Control) => current.AddMonths(-1),
                    (Key.Up, _) => current.AddDays(1),
                    (Key.Down, _) => current.AddDays(-1),
                    _ => current
                };

                if (newDate != current)
                {
                    dpSaleDate.SelectedDate = newDate;
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
            Key.OemQuestion or Key.OemQuotes or Key.OemMinus or Key.Divide => true, // / ' - 
            _ => false
        };

        private static bool TryNavigateDatePartWithTab(TextBox textBox)
        {
            // Assuming the format is dd/MM/yyyy or similar, and user selects parts manually
            // This detects current selection and moves to next logical part

            string text = textBox.Text;
            int caret = textBox.CaretIndex;
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            // Find positions of separators (assuming / as separator)
            int firstSlash = text.IndexOf('/');
            int secondSlash = text.Length > firstSlash + 1 ? text.IndexOf('/', firstSlash + 1) : -1;

            if (firstSlash == -1 || secondSlash == -1) return false;

            // Determine current part: Day (0 to firstSlash), Month (firstSlash+1 to secondSlash), Year (after secondSlash)
            if (selectionStart + selectionLength <= firstSlash)
            {
                // Currently in Day → move to Month
                textBox.Select(firstSlash + 1, secondSlash - firstSlash - 1);
                return true;
            }
            else if (selectionStart + selectionLength <= secondSlash)
            {
                // Currently in Month → move to Year
                textBox.Select(secondSlash + 1, text.Length - secondSlash - 1);
                return true;
            }
            else
            {
                // Already in Year → let Tab exit normally
                return false;
            }
        }
        private bool HandleDatePartSelection(DatePicker dp)
        {
            if (dp == null) return false;

            // 1. Get the internal TextBox of the DatePicker
            var tb = (System.Windows.Controls.Primitives.DatePickerTextBox)dp.Template.FindName("PART_TextBox", dp);
            if (tb == null) return false;

            string text = tb.Text;
            if (string.IsNullOrEmpty(text)) return false;

            // 2. Find separators (assumes dd/MM/yyyy or dd-MM-yyyy)
            int firstSep = text.IndexOfAny(new char[] { '/', '-', '.' });
            if (firstSep == -1) return false; // No separators found

            int secondSep = text.IndexOfAny(new char[] { '/', '-', '.' }, firstSep + 1);
            if (secondSep == -1) return false;

            // 3. Define ranges
            int dayStart = 0;
            int dayLen = firstSep;

            int monthStart = firstSep + 1;
            int monthLen = secondSep - monthStart;

            int yearStart = secondSep + 1;
            int yearLen = text.Length - yearStart;

            // 4. Check what is currently selected and move to next
            if (tb.SelectionLength == text.Length || tb.SelectionLength == 0)
            {
                // If everything or nothing is selected -> Select DAY
                tb.Select(dayStart, dayLen);
                return true;
            }
            else if (tb.SelectionStart == dayStart && tb.SelectionLength == dayLen)
            {
                // If Day is selected -> Select MONTH
                tb.Select(monthStart, monthLen);
                return true;
            }
            else if (tb.SelectionStart == monthStart && tb.SelectionLength == monthLen)
            {
                // If Month is selected -> Select YEAR
                tb.Select(yearStart, yearLen);
                return true;
            }

            // If Year is already selected, return false to let Focus leave the control
            return false;
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
