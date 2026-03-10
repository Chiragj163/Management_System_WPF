using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Management_System_WPF.Models;


namespace Management_System_WPF.Views
{
    public partial class ItemDetailsWindow : Window
    {
        public ItemDetailsWindow(string buyerName, string dateStr, List<SaleDetailItem> items)
        {
            InitializeComponent();

            txtContext.Text = $"{buyerName} • {dateStr}";

            var displayList = items.Select(i => new
            {
                Article = i.Article,
                Qty = i.Qty,
                Price = i.Price,
                Total = (decimal)i.Qty * (decimal)i.Price
            }).ToList();

            dgDetails.ItemsSource = displayList;

            decimal grandTotal = displayList.Sum(x => x.Total);
            txtGrandTotal.Text = grandTotal.ToString("N2"); 

            this.MouseLeftButtonDown += (s, e) => this.DragMove();
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Escape)
                {
                    this.Close();
                    e.Handled = true; 
                }
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}