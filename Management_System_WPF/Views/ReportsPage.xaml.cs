using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Management_System_WPF.Views
{
    public partial class ReportsPage : Page
    {
        private char _lastCharPressed = '\0';
        private DateTime _lastCharTime = DateTime.MinValue;
        private int _charCycleIndex = -1;
        private const int CycleTimeoutSeconds = 5;
        public ReportsPage()
        {
            InitializeComponent();
            LoadBuyers();
        }
        private string GetDisplayValue(object item, string path)
        {
            if (item == null) return "";
            if (string.IsNullOrEmpty(path)) return item.ToString();
            var prop = item.GetType().GetProperty(path);
            return prop?.GetValue(item)?.ToString() ?? "";
        }

        private List<object> GetItemsStartingWith(ComboBox cb, char letter)
        {
            var items = new List<object>();
            if (cb.ItemsSource == null) return items;

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

        private void SearchableComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null || cb.ItemsSource == null) return;

            // Ignore navigation keys so user can move through the list
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter ||
                e.Key == Key.Tab || e.Key == Key.Escape)
                return;

            var textBox = e.OriginalSource as TextBox;
            if (textBox == null) return;

            string searchText = textBox.Text;

            // Reset cycling on Backspace
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

                if (sameKey && withinTime)
                {
                    var matches = GetItemsStartingWith(cb, pressedChar);
                    if (matches.Count > 0)
                    {
                        _charCycleIndex++;
                        if (_charCycleIndex >= matches.Count) _charCycleIndex = 0;

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

                // Initial press of a letter
                _lastCharPressed = pressedChar;
                _lastCharTime = now;
                _charCycleIndex = -1;
            }

            // -------- NORMAL SEARCH FILTER --------
            // Background priority prevents the UI from stuttering during typing
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
                    return displayValue.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                };

                cb.IsDropDownOpen = true;

                // Re-focus and keep cursor at the end
                textBox.Focus();
                textBox.CaretIndex = textBox.Text.Length;

            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private void LoadBuyers()
        {
            var buyers = BuyersService
                .GetAllBuyers()
                .OrderBy(b => b.Name)
                .ToList();

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

            if (!BuyersService.BuyerExists(buyer.BuyerId))
            {
                MessageBox.Show("Selected buyer no longer exists.");
                LoadBuyers();
                return;
            }

            var main = (MainWindow)Application.Current.MainWindow;
            main.ShowFullScreenPage();
            NavigationService.Navigate(
                new BuyerReportPage(buyer.BuyerId, buyer.Name)
            );
        }

        private void SalesByArticles_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ArticleReportPage());
        }

        private void SalesByBuyer_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Application.Current.MainWindow;

            main.ShowFullScreenPage();

            NavigationService.Navigate(new SaleByBuyerPage());
        }

        private void AllSalesReport_Click(object sender, RoutedEventArgs e)
        {
            AllSales_Click(sender, e);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
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
    }
}
