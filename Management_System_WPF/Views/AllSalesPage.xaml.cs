using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Management_System_WPF.Views
{
    public partial class AllSalesPage : Page
    {
        private List<SaleRecord> _allSales = new();
        private Dictionary<string, string> _articleCategoryMap = new();

        public AllSalesPage()
        {
            InitializeComponent();
            txtTitle.Text = "All Sales";
            LoadCategories();

            LoadSales();
        }
        private void LoadCategories()
        {
            var items = ItemsService.GetAllItems();

            _articleCategoryMap = items
                .ToDictionary(i => i.Name, i => i.Category);

            var categories = new List<string> { "All" };
            categories.AddRange(items.Select(i => i.Category).Distinct().OrderBy(x => x));

            cmbCategory.ItemsSource = categories;
            cmbCategory.SelectedIndex = 0;
        }

        private void LoadSales()
        {
            _allSales = SalesService.GetAllSaleRecords();
            dgSales.ItemsSource = _allSales;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Application.Current.MainWindow;
            main.RestoreHomeLayout();
        }
        private void Filter_Changed(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;
            cmbCategory.SelectedIndex = 0;
            dgSales.ItemsSource = _allSales;
        }

        private void ApplyFilters()
        {
            IEnumerable<SaleRecord> filtered = _allSales;
            string search = txtSearch.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(s =>
                    (!string.IsNullOrWhiteSpace(s.BuyerName) &&
                     s.BuyerName.ToLower().Contains(search)) ||
                    (!string.IsNullOrWhiteSpace(s.ItemName) &&
                     s.ItemName.ToLower().Contains(search)));
            }
            if (dpFrom.SelectedDate != null)
            {
                filtered = filtered.Where(s =>
                    s.SaleDate.Date >= dpFrom.SelectedDate.Value.Date);
            }
            if (dpTo.SelectedDate != null)
            {
                filtered = filtered.Where(s =>
                    s.SaleDate.Date <= dpTo.SelectedDate.Value.Date);
            }
            if (cmbCategory.SelectedItem != null &&
                cmbCategory.SelectedItem.ToString() != "All")
            {
                string selectedCategory = cmbCategory.SelectedItem.ToString();

                filtered = filtered.Where(s =>
                    _articleCategoryMap.TryGetValue(s.ItemName, out var cat) &&
                    string.Equals(cat, selectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            dgSales.ItemsSource = filtered.ToList();
        }
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            var sale = btn?.DataContext as SaleRecord;
            if (sale == null) return;

            ContextMenu menu = new ContextMenu();

            MenuItem view = new MenuItem { Header = "View 👁" };
            view.Click += (_, __) =>
            {
                MessageBox.Show(
                    $"Customer: {sale.BuyerName}\n" +
                    $"Item: {sale.ItemName}\n" +
                    $"Qty: {sale.Quantity}\n" +
                    $"Amount: ₹{sale.Amount}\n" +
                    $"Category: {_articleCategoryMap.GetValueOrDefault(sale.ItemName, "N/A")}\n" +
                    $"Date: {sale.SaleDate:dd/MM/yyyy}",
                    "Sale Details");
            };

            MenuItem edit = new MenuItem { Header = "Edit ✏" };
            edit.Click += (_, __) =>
            {
                var win = new EditSaleWindow(sale);
                if (win.ShowDialog() == true)
                    LoadSales();
            };

            MenuItem delete = new MenuItem { Header = "Delete ❌" };
            delete.Click += (_, __) =>
            {
                if (MessageBox.Show("Delete this sale?", "Confirm",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    SalesService.DeleteSale(sale.SaleId);
                    LoadSales();
                }
            };

            menu.Items.Add(view);
            menu.Items.Add(edit);
            menu.Items.Add(delete);

            menu.IsOpen = true;
        }
        private void ToggleFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility =
                FilterPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

    }
}
