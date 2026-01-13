using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Management_System_WPF.Views
{
    public partial class BuyerDetailsWindow : Window
    {
        public BuyerDetailsWindow(string article, string date, List<(int Qty, string Buyer)> data)
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
            txtTitle.Text = $"{article} — {date}";

            dgBuyers.ItemsSource = data.Select(x => new
            {
                Qty = x.Qty,
                BuyerName = x.Buyer
            });
        }
    }
}
