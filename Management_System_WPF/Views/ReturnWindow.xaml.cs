using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;

namespace Management_System_WPF.Views
{
    public partial class ReturnWindow : Window
    {
        private readonly List<CartItem> cart = new();
        private decimal currentItemPrice = 0;
        private char _lastCharPressed = '\0';
        private DateTime _lastCharTime = DateTime.MinValue;
        private int _charCycleIndex = -1;
        private const int CycleTimeoutSeconds = 5;
        private int _buyerId;
        private int _year;
        private int _month;
        private int? _editingReturnId = null;
        public ReturnWindow(int buyerId, int year, int month)
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
            _buyerId = buyerId;
            _year = year;
            _month = month;
            // ensure we reference the ComboBox name used in XAML
            cmbItems.ItemsSource = ItemsService.GetAllItems().OrderBy(a => a.Name).ToList();
            cmbItems.DisplayMemberPath = "Name";
            LoadItems();     
            LoadReturns();  
        }
        
        // Added missing event handler referenced from XAML (safe no-op)
        private void cmbBuyer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No implementation required here for now. If you expect to react to buyer changes,
            // implement logic to update _buyerId and reload returns.
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
        private string GetDisplayValue(object item, string path)
        {
            if (item == null) return "";
            if (string.IsNullOrEmpty(path)) return item.ToString();
            var prop = item.GetType().GetProperty(path);
            return prop?.GetValue(item)?.ToString() ?? "";
        }
        private void SearchableComboBox_KeyUp(object sender, KeyEventArgs e)
        {
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
        private void LoadItems()
        {
            var allItems = ItemsService.GetAllItems().OrderBy(x => x.Name).ToList();
            cmbItems.ItemsSource = allItems;
        }

        private void LoadReturns()
        {
            var list = ReturnService.GetDetailedReturns(_buyerId, _year, _month);
            dgReturns.ItemsSource = list;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (cmbItems.SelectedItem == null) { MessageBox.Show("Select an item."); return; }
            if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0) { MessageBox.Show("Invalid Quantity."); return; }
            int itemId = (int)cmbItems.SelectedValue;

            if (_editingReturnId == null)
            {
                decimal price = SpecialPriceService.GetEffectivePrice(_buyerId, itemId);


                ReturnService.AddReturn(_buyerId, itemId, _year, _month, qty, price);
            }
            else
            {
                ReturnService.UpdateReturn(_editingReturnId.Value, itemId, qty);
                MessageBox.Show("Return Updated!");
            }

            ClearForm();
            LoadReturns(); 
        }

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            var item = ((FrameworkElement)sender).DataContext as ReturnModel;
            if (item == null) return;
            _editingReturnId = item.ReturnId;
            cmbItems.SelectedValue = item.ItemId;
            txtQty.Text = item.Qty.ToString();
            lblTitle.Text = "Edit Return";
            btnSave.Content = "Update";
            btnSave.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
        }
        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            var item = ((FrameworkElement)sender).DataContext as ReturnModel;
            if (item == null) return;

            if (MessageBox.Show("Delete this return?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ReturnService.DeleteReturn(item.ReturnId);
                LoadReturns();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            _editingReturnId = null;
            cmbItems.SelectedIndex = -1;
            txtQty.Clear();

            lblTitle.Text = "Manage Returns";
            btnSave.Content = "Save Return";
            btnSave.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E53935"));
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}