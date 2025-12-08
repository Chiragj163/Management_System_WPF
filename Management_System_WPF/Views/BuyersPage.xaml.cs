using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Management_System_WPF.Views
{
    public partial class BuyersPage : Page
    {
        private Buyer? selectedBuyer = null;

        public BuyersPage()
        {
            InitializeComponent();
            LoadBuyers();
        }

        // ------------------- LOAD BUYERS -------------------
        private void LoadBuyers()
        {
            var buyers = BuyersService.GetAllBuyers();

            if (buyers == null)
                return;

            int count = 1;
            foreach (var buyer in buyers)
            {
                buyer.SerialNumber = count++;
            }

            dgBuyers.ItemsSource = buyers;
        }

        // ------------------- EXIT PAGE -------------------
        private void ExitBuyerPage_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow main)
                main.MainFrame.Content = null;
        }

        // ------------------- OPTIONS: EDIT / DELETE -------------------
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not Buyer buyer)
                return;

            ContextMenu menu = new ContextMenu();

            MenuItem edit = new MenuItem { Header = "Edit Buyer ✏" };
            edit.Click += (s, ev) => EditBuyer(buyer);

            MenuItem delete = new MenuItem { Header = "Delete Buyer ❌" };
            delete.Click += (s, ev) => DeleteBuyer(buyer);

            menu.Items.Add(edit);
            menu.Items.Add(delete);

            menu.IsOpen = true;
        }

        // ------------------- EDIT BUYER -------------------
        private void EditBuyer(Buyer buyer)
        {
            if (buyer == null)
                return;

            selectedBuyer = buyer;

            txtBuyerName.Text = buyer.Name;
            btnSave.Content = "Update Buyer";
        }

        // ------------------- DELETE BUYER -------------------
        private void DeleteBuyer(Buyer buyer)
        {
            if (buyer == null)
                return;

            if (MessageBox.Show(
                    $"Delete buyer '{buyer.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                ) == MessageBoxResult.Yes)
            {
                BuyersService.DeleteBuyer(buyer.BuyerId);
                LoadBuyers();
            }
        }

        // ------------------- SAVE / UPDATE BUYER -------------------
        private void SaveBuyer_Click(object sender, RoutedEventArgs e)
        {
            string name = txtBuyerName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter buyer name.");
                return;
            }

            if (selectedBuyer == null)
            {
                // ADD NEW BUYER
                BuyersService.AddBuyer(new Buyer
                {
                    Name = name
                });
            }
            else
            {
                // UPDATE EXISTING BUYER
                selectedBuyer.Name = name;
                BuyersService.UpdateBuyer(selectedBuyer);

                selectedBuyer = null;
                btnSave.Content = "Save Buyer"; // Reset UI
            }

            txtBuyerName.Text = "";
            LoadBuyers();
            Keyboard.ClearFocus();
        }
    }
}
