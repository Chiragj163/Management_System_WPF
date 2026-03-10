using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Management_System_WPF.Views
{
    public partial class ReturnWindow : Window
    {
        private int _buyerId;
        private int _year;
        private int _month;

        // Tracks if we are editing (not null) or adding (null)
        private int? _editingReturnId = null;

        // Constructor receives context to load data
        public ReturnWindow(int buyerId, int year, int month)
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
            _buyerId = buyerId;
            _year = year;
            _month = month;

            LoadItems();     // Fill Dropdown
            LoadReturns();   // Fill Grid
        }

        // 🟢 LOAD DROPDOWN
        private void LoadItems()
        {
            var allItems = ItemsService.GetAllItems().OrderBy(x => x.Name).ToList();
            cmbItems.ItemsSource = allItems;
        }

        // 🟢 LOAD GRID
        private void LoadReturns()
        {
            var list = ReturnService.GetDetailedReturns(_buyerId, _year, _month);
            dgReturns.ItemsSource = list;
        }

        // 💾 SAVE (ADD OR UPDATE)
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (cmbItems.SelectedItem == null) { MessageBox.Show("Select an item."); return; }
            if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0) { MessageBox.Show("Invalid Quantity."); return; }

            // Get selected Item ID
            int itemId = (int)cmbItems.SelectedValue;

            if (_editingReturnId == null)
            {
                // INSERT NEW
                ReturnService.AddReturn(_buyerId, itemId, _year, _month, qty);
                MessageBox.Show("Return Added!");
            }
            else
            {
                // UPDATE EXISTING
                ReturnService.UpdateReturn(_editingReturnId.Value, itemId, qty);
                MessageBox.Show("Return Updated!");
            }

            // Reset UI
            ClearForm();
            LoadReturns(); // Refresh List
        }

        // ✏️ EDIT ROW (Clicked from Grid)
        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            var item = ((FrameworkElement)sender).DataContext as ReturnModel;
            if (item == null) return;

            // Populate Form
            _editingReturnId = item.ReturnId;
            cmbItems.SelectedValue = item.ItemId;
            txtQty.Text = item.Qty.ToString();

            // Change UI State
            lblTitle.Text = "Edit Return";
            btnSave.Content = "Update";
            btnSave.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
        }

        // 🗑️ DELETE ROW
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

        // 🧹 CLEAR FORM
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
            DialogResult = true; // Tell parent to refresh
            Close();
        }
    }
}