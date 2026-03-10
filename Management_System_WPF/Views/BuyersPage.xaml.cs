using System;
using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
            var buyers = BuyersService
                .GetAllBuyers()
                .OrderBy(b => b.Name)   // ✅ A → Z sorting
                .ToList();

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

            MenuItem edit = new MenuItem { Header = "Manage Buyer ✏" };
            edit.Click += (s, ev) =>
            {
                var win = new BuyerSpecialPriceWindow(buyer)
                {
                    Owner = Application.Current.MainWindow
                };
                win.ShowDialog();
                LoadBuyers();
            };


            MenuItem delete = new MenuItem { Header = "Delete Buyer ❌" };
            delete.Click += (s, ev) => DeleteBuyer(buyer);

            menu.Items.Add(edit);
            menu.Items.Add(delete);

            menu.IsOpen = true;
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
                MessageBox.Show("Buyer Name Added Successfully!");
            }
            else
            {
                // UPDATE EXISTING BUYER
                selectedBuyer.Name = name;
                BuyersService.UpdateBuyer(selectedBuyer);

                selectedBuyer = null;
                btnSave.Content = "Save Buyer";
            }

            txtBuyerName.Text = "";
            LoadBuyers();

            // ✅ MOVE CURSOR BACK TO TEXTBOX
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtBuyerName.Focus();
                txtBuyerName.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void Buyer_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgBuyers.SelectedItem is Buyer buyer)
            {
                BuyerSpecialPriceWindow win =
                    new BuyerSpecialPriceWindow(buyer);

                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
            }
        }
        private void txtSearchBuyer_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 1. Get the view
            var view = CollectionViewSource.GetDefaultView(dgBuyers.ItemsSource);

            // 2. Guard clause: If the view is null, stop immediately
            if (view == null) return;

            // 3. Optimize: Grab the search text once to avoid accessing the UI property repeatedly
            string searchText = txtSearchBuyer.Text;

            view.Filter = item =>
            {
                // Show all if search is empty
                if (string.IsNullOrEmpty(searchText))
                    return true;

                // Safe cast
                if (item is Buyer buyer)
                {
                    // CRITICAL FIX: Check if buyer.Name is null before calling IndexOf
                    return !string.IsNullOrEmpty(buyer.Name) &&
                           buyer.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                return false; // Hide items that aren't Buyers
            };

            // Optional: Refresh isn't usually needed as setting Filter triggers it, 
            // but sometimes required if the view state gets stale.
            // view.Refresh(); 
        }
    }
}
