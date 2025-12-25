using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Windows;

namespace Management_System_WPF.Views
{
    public partial class EditSaleWindow : Window
    {
        private readonly SaleRecord _sale;

        // ✅ THIS CONSTRUCTOR MUST EXIST
        public EditSaleWindow(SaleRecord sale)
        {
            InitializeComponent();

            _sale = sale;

            txtBuyer.Text = sale.BuyerName;
            txtItem.Text = sale.ItemName;
            txtQty.Text = sale.Qty.ToString();
            dpSaleDate.SelectedDate = sale.SaleDate; 
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtQty.Text, out int qty))
            {
                MessageBox.Show("Invalid quantity");
                return;
            }

            SalesService.UpdateSaleQty(_sale.SaleId, _sale.ItemId, qty);
            SalesService.UpdateSaleDate(_sale.SaleId, dpSaleDate.SelectedDate.Value);
            DialogResult = true;
            Close();
        }
    }
}
