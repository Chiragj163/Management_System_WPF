using Management_System.Views;
using Management_System_WPF.Views;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Management_System_WPF
{
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

        // ------------------ PAGE NAVIGATION ------------------

        private void Sales_Click(object sender, RoutedEventArgs e)
        {
            ResetLayoutBeforeNavigation();
            MainFrame.Navigate(new SalesPage());
        }

        private void Inventory_Click(object sender, RoutedEventArgs e)
        {
            ResetLayoutBeforeNavigation();
            MainFrame.Navigate(new InventoryPage());
        }

        private void Buyers_Click(object sender, RoutedEventArgs e)
        {
            ResetLayoutBeforeNavigation();
            MainFrame.Navigate(new BuyersPage());
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            ResetLayoutBeforeNavigation();
            MainFrame.Navigate(new ReportsPage()); // later
        }

        private void AllSales_Click(object sender, RoutedEventArgs e)
        {
            ShowFullScreenPage();
            MainFrame.Navigate(new AllSalesPage());
        }

        // ------------------ LAYOUT CONTROL ------------------

        public void ResetLayoutBeforeNavigation()
        {
            SideMenu.Visibility = Visibility.Visible;
            MainLayout.ColumnDefinitions[0].Width = new GridLength(400);

            MainFrame.Margin = new Thickness(240, 90, 20, 20);
        }

        public void ShowFullScreenPage()
        {
            SideMenu.Visibility = Visibility.Collapsed;
            MainLayout.ColumnDefinitions[0].Width = new GridLength(0);

            MainFrame.Margin = new Thickness(0, 90, 0, 20);
        }

        public void RestoreHomeLayout()
        {
            SideMenu.Visibility = Visibility.Visible;
            MainLayout.ColumnDefinitions[0].Width = new GridLength(400);

            MainFrame.Margin = new Thickness(240, 90, 20, 20);

            MainFrame.Content = null;   // Clears page
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
