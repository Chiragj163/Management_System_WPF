using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Management_System_WPF.Views
{
    public partial class BuyersPage : Page
    {
        private Buyer? selectedBuyer = null;  // For edit mode

        public BuyersPage()
        {
            InitializeComponent();
            LoadBuyers();
        }

        private void LoadBuyers()
        {
            var buyers = BuyersService.GetAllBuyers();

            int count = 1;
            foreach (var b in buyers)
            {
                b.SerialNumber = count++;
            }

            dgBuyers.ItemsSource = buyers;
        }

        // EXIT PAGE
        private void ExitBuyerPage_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).MainFrame.Content = null;
        }

        // OPTIONS MENU (EDIT / DELETE)
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Buyer buyer = button.DataContext as Buyer;

            ContextMenu menu = new ContextMenu();

            MenuItem edit = new MenuItem { Header = "Edit Buyer ✏" };
            edit.Click += (s, ev) => EditBuyer(buyer);

            MenuItem delete = new MenuItem { Header = "Delete Buyer ❌" };
            delete.Click += (s, ev) => DeleteBuyer(buyer);

            menu.Items.Add(edit);
            menu.Items.Add(delete);

            menu.IsOpen = true;
        }

        // EDIT BUYER
        private void EditBuyer(Buyer buyer)
        {
            selectedBuyer = buyer;

            txtBuyerName.Text = buyer.Name;
            btnSave.Content = "Update Buyer";
        }

        // DELETE BUYER
        private void DeleteBuyer(Buyer buyer)
        {
            if (MessageBox.Show($"Delete buyer '{buyer.Name}'?",
                                "Confirm Delete", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                BuyersService.DeleteBuyer(buyer.BuyerId);
                LoadBuyers();
            }
        }

        // SAVE + UPDATE BUYER
        private void SaveBuyer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuyerName.Text))
            {
                MessageBox.Show("Please enter buyer name.");
                return;
            }

            if (selectedBuyer == null)
            {
                // Add new buyer
                BuyersService.AddBuyer(new Buyer
                {
                    Name = txtBuyerName.Text
                });
            }
            else
            {
                // Update existing buyer
                selectedBuyer.Name = txtBuyerName.Text;
                BuyersService.UpdateBuyer(selectedBuyer);

                selectedBuyer = null;
                btnSave.Content = "Save Buyer";  // Reset button text
            }

            txtBuyerName.Text = "";
            LoadBuyers();
            Keyboard.ClearFocus();
        }

    }
}
