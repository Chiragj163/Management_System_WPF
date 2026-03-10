using Management_System_WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Management_System_WPF.Views
{
    /// <summary>
    /// Interaction logic for ItemPriceHistoryWindow.xaml
    /// </summary>
    public partial class ItemPriceHistoryWindow : Window
    {
        public ItemPriceHistoryWindow(List<ItemPriceHistory> history)
        {
            InitializeComponent();
            DataContext = history;
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}
