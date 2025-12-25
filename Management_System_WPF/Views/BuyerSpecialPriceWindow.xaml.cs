using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Management_System_WPF.Views
{
    public partial class BuyerSpecialPriceWindow : Window
    {
        private Buyer _buyer;

        public ObservableCollection<SpecialPriceVM> SpecialPrices { get; set; }
            = new ObservableCollection<SpecialPriceVM>();

        public BuyerSpecialPriceWindow(Buyer buyer)
        {
            InitializeComponent();

            _buyer = buyer;

            txtBuyerName.Text = buyer.Name;

            try
            {
                LoadSpecialPrices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }



        // =========================================================
        // LOAD SPECIAL PRICES
        // =========================================================
        private void LoadSpecialPrices()
        {
            try
            {
                SpecialPrices.Clear();

                var prices = SpecialPriceService
                    .GetBuyerRelevantArticles(_buyer.BuyerId);

                foreach (var p in prices)
                    SpecialPrices.Add(p);

                dgSpecialPrices.ItemsSource = SpecialPrices;

                // 🔥 ALSO UPDATE COMBOBOX HERE
                cmbItems.ItemsSource = ItemsService.GetAllItems();
                cmbItems.DisplayMemberPath = "Name";
                cmbItems.SelectedValuePath = "Id";

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error");
            }
        }



        // =========================================================
        // SAVE BUYER NAME
        // =========================================================
        private void SaveBuyerName_Click(object sender, RoutedEventArgs e)
        {
            string name = txtBuyerName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Buyer name cannot be empty");
                return;
            }

            _buyer.Name = name;
            BuyersService.UpdateBuyer(_buyer);

            MessageBox.Show("Buyer name updated");
        }


        // =========================================================
        // SAVE NEW SPECIAL PRICE
        // =========================================================
        private void SaveSpecialPrice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbItems.SelectedValue == null)
                {
                    MessageBox.Show("Select an article");
                    return;
                }

                if (!decimal.TryParse(txtSpecialPrice.Text, out decimal price))
                {
                    MessageBox.Show("Invalid price");
                    return;
                }

                int itemId = (int)cmbItems.SelectedValue;

                SpecialPriceService.SaveOrUpdate(_buyer.BuyerId, itemId, price);

                txtSpecialPrice.Text = "";
                cmbItems.SelectedIndex = -1;

                LoadSpecialPrices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error");
            }
        }


        // =========================================================
        // UPDATE PRICE FROM GRID
        // =========================================================
        private void UpdateSpecialPrice_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not SpecialPriceVM vm)
                return;

            SpecialPriceService.SaveOrUpdate(
                _buyer.BuyerId,
                vm.ItemId,
                vm.SpecialPrice
            );

            MessageBox.Show("Special price updated");
        }
        private void DeleteBuyer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                $"Delete buyer '{_buyer.Name}'?\nAll special prices will be removed.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            BuyersService.DeleteBuyer(_buyer.BuyerId);

            MessageBox.Show("Buyer deleted successfully");

            DialogResult = true;
            Close();
        }
        private void DeleteSpecialPrice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe)
                return;

            if (fe.DataContext is not SpecialPriceVM vm)
            {
                MessageBox.Show("Row binding failed");
                return;
            }

            if (MessageBox.Show(
                $"Remove special price for '{vm.ItemName}'?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                SpecialPriceService.Delete(_buyer.BuyerId, vm.ItemId);
                LoadSpecialPrices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error");
            }
        }




    }
}
