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
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
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


        private void SaveSpecialPrice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbItems.SelectedValue == null) { /* ... check code ... */ return; }
                if (!decimal.TryParse(txtSpecialPrice.Text, out decimal price)) { /* ... check code ... */ return; }

                int itemId = (int)cmbItems.SelectedValue;

                // 1. Save the new Special Price for future sales
                SpecialPriceService.SaveOrUpdate(_buyer.BuyerId, itemId, price);

                // 2. 🔥 NEW: Update all PAST sales for this buyer/item combo
                SalesService.UpdatePastSalePrices(_buyer.BuyerId, itemId, price);

                txtSpecialPrice.Text = "";
                cmbItems.SelectedIndex = -1;
                LoadSpecialPrices();

                MessageBox.Show("Special price updated and applied to all historical records.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error");
            }
        }


        private void UpdateSpecialPrice_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not SpecialPriceVM vm)
                return;

            decimal newPrice = vm.SpecialPrice ?? vm.OriginalPrice;

            // Save for future
            SpecialPriceService.SaveOrUpdate(_buyer.BuyerId, vm.ItemId, newPrice);

            // 🔥 Update the past
            SalesService.UpdatePastSalePrices(_buyer.BuyerId, vm.ItemId, newPrice);

            MessageBox.Show("Special price and past records updated.");
        }

        private void DeleteBuyer_Click(object sender, RoutedEventArgs e)
        {
            DeleteBuyer(_buyer);
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


        // Add this method inside the BuyerSpecialPriceWindow class
        private void DeleteBuyer(Buyer buyer)
        {
            if (buyer == null)
                return;

            if (MessageBox.Show(
                    $"Delete buyer '{buyer.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                ) != MessageBoxResult.Yes)
                return;

            try
            {
                // DIRECTLY DELETE WITHOUT PASSWORD
                BuyersService.DeleteBuyer(buyer.BuyerId);
                MessageBox.Show("Buyer deleted");

                // Note: caller (DeleteBuyer_Click) closes the window and refreshes lists as needed
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error");
            }
        }

    }
}
