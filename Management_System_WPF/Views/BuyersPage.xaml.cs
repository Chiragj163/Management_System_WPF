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

        private void LoadBuyers()
        {
            var buyers = BuyersService
                .GetAllBuyers()
                .OrderBy(b => b.Name)  
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

        private void ExitBuyerPage_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow main)
                main.MainFrame.Content = null;
        }
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
                BuyersService.AddBuyer(new Buyer
                {
                    Name = name
                });
                MessageBox.Show("Buyer Name Added Successfully!");
            }
            else
            {
                selectedBuyer.Name = name;
                BuyersService.UpdateBuyer(selectedBuyer);

                selectedBuyer = null;
                btnSave.Content = "Save Buyer";
            }

            txtBuyerName.Text = "";
            LoadBuyers();
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
            var view = CollectionViewSource.GetDefaultView(dgBuyers.ItemsSource);

            if (view == null) return;
            string searchText = txtSearchBuyer.Text;

            view.Filter = item =>
            {
                if (string.IsNullOrEmpty(searchText))
                    return true;
                if (item is Buyer buyer)
                {
                    return !string.IsNullOrEmpty(buyer.Name) &&
                           buyer.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                return false;
            };
        }
    }
}
