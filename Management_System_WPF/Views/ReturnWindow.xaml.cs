using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Management_System_WPF.Views
{
    public partial class ReturnWindow : Window
    {
        public string SelectedItemName { get; private set; }
        public int ReturnQty { get; private set; }
        public int SelectedItemId { get; private set; }

        // We keep the parameter 'sales' to avoid breaking the calling code, 
        // but we won't use it for the dropdown source.
        public ReturnWindow(List<SalesRaw> sales)
        {
            InitializeComponent();

            // ✅ CHANGED: Load ALL items from the database instead of filtering the sales list
            var allItems = ItemsService.GetAllItems()
                                       .OrderBy(x => x.Name)
                                       .ToList();

            cmbItems.ItemsSource = allItems;

            // Assuming your Item model has 'Name' property. 
            // If it is 'ItemName', change this string accordingly.
            cmbItems.DisplayMemberPath = "Name";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validate Selection
            if (cmbItems.SelectedItem == null)
            {
                MessageBox.Show("Select an item.");
                return;
            }

            // 2. Validate Quantity
            if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Invalid Quantity.");
                return;
            }

            // 3. Get Data from Selected Item
            // Since ItemsSource is List<Item>, SelectedItem is an 'Item' object
            var selectedItem = cmbItems.SelectedItem as Item;

            if (selectedItem != null)
            {
                SelectedItemId = selectedItem.Id;
                SelectedItemName = selectedItem.Name;
                ReturnQty = qty;

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Error selecting item.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}