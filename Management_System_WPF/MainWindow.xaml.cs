using Management_System_WPF.Views;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace Management_System_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StartClock();
        }
        private void StartClock()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                txtDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
                txtTime.Text = DateTime.Now.ToString("hh:mm:ss tt");
            };
            timer.Start();
        }

        private void Sales_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SalesPage());
        }


        private void Inventory_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new InventoryPage());
        }


        private void Buyers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Management_System_WPF.Views.BuyersPage());
        }


        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Reports page
        }

        private void AllSales_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to All Sales page
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
       
    }
}




















