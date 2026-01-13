using System.Windows;
using System.Windows.Controls;
using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows.Data; // Required for CollectionView

namespace Management_System_WPF.Views
{
    public partial class InventoryPage : Page
    {
        private Item selectedItem = null;
        private List<Item> _allItems = new();

        public InventoryPage()
        {
            InitializeComponent();
            LoadCategories();
            LoadItems();
        }

        private void LoadCategories()
        {
            var categories = new List<string> { "Double Station", "Vertical", "Rotary" };

            // Input ComboBox
            cmbCategory.ItemsSource = categories;

            // Filter ComboBox
            var filterOptions = new List<string> { "All" };
            filterOptions.AddRange(categories);
            cmbFilterCategory.ItemsSource = filterOptions;
            cmbFilterCategory.SelectedIndex = 0;
        }

        private void LoadItems()
        {
            _allItems = ItemsService.GetAllItems();
            dgItems.ItemsSource = _allItems;

            // Apply filtering immediately after loading (in case inputs are pre-filled)
            ApplyFilters();
        }

        // ============================================
        // ✅ NEW: UNIFIED FILTERING LOGIC
        // ============================================

        private void txtSearchItem_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterCategory_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            // Ensure data exists before filtering
            if (dgItems.ItemsSource == null) return;

            // Get the Default View (WPF's way of filtering without reloading the list)
            var view = CollectionViewSource.GetDefaultView(dgItems.ItemsSource);
            if (view == null) return;

            view.Filter = item =>
            {
                var product = item as Item;
                if (product == null) return false;

                // 1. Check Category
                bool matchesCategory = true;
                if (cmbFilterCategory.SelectedItem != null)
                {
                    string selectedCat = cmbFilterCategory.SelectedItem.ToString();
                    if (!string.IsNullOrEmpty(selectedCat) && selectedCat != "All")
                    {
                        // Case-insensitive comparison
                        matchesCategory = string.Equals(product.Category, selectedCat, StringComparison.OrdinalIgnoreCase);
                    }
                }

                // 2. Check Search Text (Name)
                bool matchesText = true;
                if (!string.IsNullOrWhiteSpace(txtSearchItem.Text))
                {
                    // Case-insensitive search
                    matchesText = product.Name.IndexOf(txtSearchItem.Text, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                // Show item ONLY if BOTH conditions are true
                return matchesCategory && matchesText;
            };
        }

        // ============================================
        // CRUD OPERATIONS (Existing Code Preserved)
        // ============================================

        private void SaveItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. Basic Validation
            if (string.IsNullOrWhiteSpace(txtItemName.Text)) { MessageBox.Show("Enter item name"); return; }
            if (!decimal.TryParse(txtItemPrice.Text, out decimal price)) { MessageBox.Show("Enter valid price"); return; }
            if (string.IsNullOrWhiteSpace(cmbCategory.Text)) { MessageBox.Show("Select category"); return; }

            string itemName = txtItemName.Text.Trim(); // Trim whitespace
            string category = cmbCategory.Text;

            // 2. CHECK FOR DUPLICATES (Case-Insensitive)
            // We check against _allItems which is already loaded in memory
            bool exists = _allItems.Any(x => x.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

            if (selectedItem != null)
            {
                // UPDATE LOGIC
                // Check duplicate ONLY if name changed (allow saving same name if editing price/category)
                if (!selectedItem.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) && exists)
                {
                    MessageBox.Show("An article with this name already exists!", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selectedItem.Name = itemName;
                selectedItem.Price = price;
                selectedItem.Category = category;
                ItemsService.UpdateItem(selectedItem);
                MessageBox.Show("Item Updated!");
                selectedItem = null;
                btnSave.Content = "Save Article";
            }
            else
            {
                // ADD NEW LOGIC
                if (exists)
                {
                    MessageBox.Show("An article with this name already exists!", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var item = new Item { Name = itemName, Price = price, Category = category };
                ItemsService.AddItem(item);
                MessageBox.Show("Item Added!");
            }

            // Reload and Re-apply filters
            _allItems = ItemsService.GetAllItems();
            dgItems.ItemsSource = _allItems;
            ApplyFilters();

            ResetForm();
        }

        private void EditItem(Item item)
        {
            if (item == null) return;
            selectedItem = item;
            txtItemName.Text = item.Name;
            txtItemPrice.Text = item.Price.ToString();
            cmbCategory.Text = item.Category;
            btnSave.Content = "Update Article";
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as Item;
            if (item != null) EditItem(item);
        }

        private void DeleteItem(Item item)
        {
            if (item == null) return;
            if (MessageBox.Show("Delete this article?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ItemsService.DeleteItem(item.Id);

                // Refresh list and keep filters active
                _allItems = ItemsService.GetAllItems();
                dgItems.ItemsSource = _allItems;
                ApplyFilters();
            }
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Item item = button.DataContext as Item;
            ContextMenu menu = new ContextMenu();

            MenuItem edit = new MenuItem { Header = "Edit Article ✏" };
            edit.Click += (s, ev) => EditItem(item);

            MenuItem delete = new MenuItem { Header = "Delete Article ❌" };
            delete.Click += (s, ev) => DeleteItem(item);

            menu.Items.Add(edit);
            menu.Items.Add(delete);
            menu.IsOpen = true;
        }

        private void ExitInventoryPage_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null) mainWindow.MainFrame.Content = null;
        }

        private void ResetForm()
        {
            txtItemName.Text = "";
            txtItemPrice.Text = "";
            cmbCategory.SelectedIndex = -1;
            cmbCategory.Text = "";
            selectedItem = null;
            btnSave.Content = "Save Article";

            // ✅ FORCE CURSOR BACK TO ITEM NAME
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtItemName.Focus();
                txtItemName.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

    }
}