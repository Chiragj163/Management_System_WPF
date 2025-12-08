using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System.Windows;
using System.Windows.Controls;

namespace Management_System_WPF.Views
{
    public partial class AllSalesPage : Page
    {
        public AllSalesPage()
        {
            InitializeComponent();
            LoadSales();
        }

        private void LoadSales()
        {
            dgSales.ItemsSource = SalesService.GetAllSaleRecords();
        }

        private void ViewSale_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not SaleRecord sale)
            {
                MessageBox.Show("Sale not found.");
                return;
            }

            MessageBox.Show(
                $"Customer: {sale.BuyerName}\n" +
                $"Item: {sale.ItemName}\n" +
                $"Qty: {sale.Quantity}\n" +
                $"Amount: ₹{sale.Amount}\n" +
                $"Date: {sale.SaleDate}",
                "Sale Details");
        }

        private void EditSale_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Edit feature coming soon!");
        }

        private void DeleteSale_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not SaleRecord sale)
            {
                MessageBox.Show("Sale not found.");
                return;
            }

            if (MessageBox.Show("Delete this sale?", "Confirm", MessageBoxButton.YesNo)
                != MessageBoxResult.Yes) return;

            SalesService.DeleteSale(sale.SaleId);
            LoadSales();
        }
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            var sale = btn.DataContext as SaleRecord;

            ContextMenu menu = new ContextMenu();

            MenuItem view = new MenuItem { Header = "View 👁" };
            view.Click += (s, ev) => ViewSale_Click(btn, ev);

            MenuItem edit = new MenuItem { Header = "Edit ✏" };
            edit.Click += (s, ev) => EditSale_Click(btn, ev);

            MenuItem delete = new MenuItem { Header = "Delete ❌" };
            delete.Click += (s, ev) => DeleteSale_Click(btn, ev);

            menu.Items.Add(view);
            menu.Items.Add(edit);
            menu.Items.Add(delete);

            menu.IsOpen = true;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Application.Current.MainWindow;
            main.RestoreHomeLayout();
        }








    }
}
