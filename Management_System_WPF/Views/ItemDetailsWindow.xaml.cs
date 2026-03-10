using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Management_System_WPF.Models;


namespace Management_System_WPF.Views
{
    public partial class ItemDetailsWindow : Window
    {
        // Constructor accepting the specific list type
        public ItemDetailsWindow(string buyerName, string dateStr, List<SaleDetailItem> items)
        {
            InitializeComponent();

            // 1. Set Context Title
            txtContext.Text = $"{buyerName} • {dateStr}";

            // 2. Prepare Data (Calculate Total per line)
            var displayList = items.Select(i => new
            {
                Article = i.Article,
                Qty = i.Qty,
                Price = i.Price,
                Total = (decimal)i.Qty * (decimal)i.Price
            }).ToList();

            // 3. Bind to Grid
            dgDetails.ItemsSource = displayList;

            // 4. Calculate Grand Total
            decimal grandTotal = displayList.Sum(x => x.Total);
            txtGrandTotal.Text = grandTotal.ToString("N2"); // e.g., "1,250.00"

            // 5. Allow Dragging
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Escape)
                {
                    this.Close();
                    e.Handled = true; // Marks the key press as handled
                }
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}