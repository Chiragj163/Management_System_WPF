using System.Windows;
using System.Windows.Controls;
using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows.Data; 

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
            var categories = new List<string> { "Double Station", "Vertical", "Rotary" ,"NEW" };

           
            cmbCategory.ItemsSource = categories;
            var filterOptions = new List<string> { "All" };
            filterOptions.AddRange(categories);
            cmbFilterCategory.ItemsSource = filterOptions;
            cmbFilterCategory.SelectedIndex = 0;
        }

        private void LoadItems()
        {
            _allItems = ItemsService  .GetAllItems() .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase) .ToList();

            dgItems.ItemsSource = _allItems;
            ApplyFilters();

        }

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
            if (dgItems.ItemsSource == null) return;
            var view = CollectionViewSource.GetDefaultView(dgItems.ItemsSource);
            if (view == null) return;

            view.Filter = item =>
            {
                var product = item as Item;
                if (product == null) return false;
                bool matchesCategory = true;
                if (cmbFilterCategory.SelectedItem != null)
                {
                    string selectedCat = cmbFilterCategory.SelectedItem.ToString();
                    if (!string.IsNullOrEmpty(selectedCat) && selectedCat != "All")
                    {
                        matchesCategory = string.Equals(product.Category, selectedCat, StringComparison.OrdinalIgnoreCase);
                    }
                }
                bool matchesText = true;
                if (!string.IsNullOrWhiteSpace(txtSearchItem.Text))
                {
                    matchesText = product.Name.IndexOf(txtSearchItem.Text, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                return matchesCategory && matchesText;
            };
        }

        private void SaveItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtItemName.Text)) { MessageBox.Show("Enter item name"); return; }
            if (!decimal.TryParse(txtItemPrice.Text, out decimal price)) { MessageBox.Show("Enter valid price"); return; }
            if (string.IsNullOrWhiteSpace(cmbCategory.Text)) { MessageBox.Show("Select category"); return; }

            string itemName = txtItemName.Text.Trim();
            string category = cmbCategory.Text;

            bool exists = _allItems.Any(x => x.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

            if (selectedItem != null)
            {
                if (!selectedItem.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) && exists)
                {
                    MessageBox.Show("An article with this name already exists!", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                decimal oldPrice = selectedItem.Price;

                if (oldPrice != price)
                {
                    ItemPriceHistoryService.AddHistory(new ItemPriceHistory
                    {
                        ItemId = selectedItem.Id,
                        OldPrice = oldPrice,
                        NewPrice = price,
                        ChangedOn = DateTime.Now
                    });
                }

                selectedItem.Price = price;
                selectedItem.Category = category;
                selectedItem.Name = itemName;

                ItemsService.UpdateItem(selectedItem);

                
               
                MessageBox.Show("Item Updated!");
                selectedItem = null;
                btnSave.Content = "Save Article";
            }
            else
            {
                if (exists)
                {
                    MessageBox.Show("An article with this name already exists!", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var item = new Item { Name = itemName, Price = price, Category = category };
                ItemsService.AddItem(item);
                MessageBox.Show("Item Added Successfully!");
            }

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



        private void DeleteItem(Item item)
        {
            if (item == null) return;
            if (MessageBox.Show("Delete this article?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                
                ItemsService.DeleteItem(item.Id);

               
                _allItems = ItemsService.GetAllItems();
                dgItems.ItemsSource = _allItems;
                ApplyFilters();

                if (selectedItem == item)
                    ClearInputs();
            }
        }
        private void ClearInputs()
        {
            txtItemName.Text = "";
            txtItemPrice.Text = "";
            cmbCategory.SelectedIndex = -1;
            selectedItem = null;
            btnSave.Content = "Add Article";
        }
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Item item = button.DataContext as Item;
            if (item == null) return;
            ContextMenu menu = new ContextMenu();

            MenuItem edit = new MenuItem { Header = "Edit Article ✏" };
            edit.Click += (s, ev) => EditItem(item);

            MenuItem history = new MenuItem { Header = "View Price History 📊" };
            history.Click += (s, ev) => ViewPriceHistory(item);
            MenuItem delete = new MenuItem { Header = "Delete Article ❌" };
            delete.Click += (s, ev) => DeleteItem(item);

            menu.Items.Add(edit);
            menu.Items.Add(history);
            menu.Items.Add(delete);
            menu.IsOpen = true;
        }
        private void ViewPriceHistory(Item item)
        {
            var history = ItemPriceHistoryService.GetHistory(item.Id);

            if (history == null || history.Count == 0)
            {
                MessageBox.Show("No price history found.");
                return;
            }

            var window = new ItemPriceHistoryWindow(history);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
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
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtItemName.Focus();
                txtItemName.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
       

    }
}