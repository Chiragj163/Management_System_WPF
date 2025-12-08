using System.Windows;
using System.Windows.Controls;
using Management_System_WPF.Models;
using Management_System_WPF.Services;

namespace Management_System_WPF.Views
{
    public partial class InventoryPage : Page
    {
        private Item selectedItem = null; // <-- TRACK SELECTED ITEM FOR EDITING

        public InventoryPage()
        {
            InitializeComponent();
            LoadItems();
        }

        private void LoadItems()
        {
            dgItems.ItemsSource = ItemsService.GetAllItems();
        }

        // SAVE or UPDATE BUTTON
        private void SaveItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                MessageBox.Show("Enter item name");
                return;
            }

            if (!decimal.TryParse(txtItemPrice.Text, out decimal price))
            {
                MessageBox.Show("Enter valid price");
                return;
            }

            // ---------- UPDATE EXISTING ----------
            if (selectedItem != null)
            {
                selectedItem.Name = txtItemName.Text;
                selectedItem.Price = price;

                ItemsService.UpdateItem(selectedItem);  // CALL UPDATE SERVICE
                MessageBox.Show("Item Updated Successfully!");

                selectedItem = null;
                btnSave.Content = "Save Article";
            }
            else
            {
                // ---------- ADD NEW ----------
                var item = new Item
                {
                    Name = txtItemName.Text,
                    Price = price
                };

                ItemsService.AddItem(item);
                MessageBox.Show("Item Added Successfully!");
            }

            LoadItems();

            txtItemName.Text = "";
            txtItemPrice.Text = "";
        }

        // ------------------ EDIT ITEM ------------------
        private void EditItem(Item item)
        {
            if (item == null) return;

            selectedItem = item;  // Store selected item

            txtItemName.Text = item.Name;
            txtItemPrice.Text = item.Price.ToString();

            btnSave.Content = "Update Article"; // Change button text
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as Item;
            if (item != null)
                EditItem(item);
        }

        // ------------------ DELETE ITEM ------------------
        private void DeleteItem(Item item)
        {
            if (item == null) return;

            if (MessageBox.Show("Delete this article?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ItemsService.DeleteItem(item.Id);
                LoadItems();
            }
        }

        // ------------------ OPTIONS MENU ------------------
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

        // EXIT BUTTON
        private void ExitInventoryPage_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainFrame.Content = null;
            }
        }

    }
}
