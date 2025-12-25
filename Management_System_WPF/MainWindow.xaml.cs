using Management_System_WPF.Views;
using OfficeOpenXml;
using PdfSharp.Drawing;
using System;
using System.Drawing.Printing;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Management_System_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Height = SystemParameters.PrimaryScreenHeight;
            Width = SystemParameters.PrimaryScreenWidth;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
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
            MainFrame.Navigate(new ReportsPage());
        }

        private void AllSales_Click(object sender, RoutedEventArgs e)
        {
            ShowFullScreenPage();
            MainFrame.Navigate(new AllSalesPage());
        }




        // ------------------ LAYOUT CONTROL ------------------

        public void ResetLayoutBeforeNavigation()
        {
            // 1. Show the Top Bar and Side Menu again
            TopBarGrid.Visibility = Visibility.Visible;
            SideMenu.Visibility = Visibility.Visible;

            // 2. Restore the Side Menu Column width
            MainLayout.ColumnDefinitions[0].Width = new GridLength(400);

            // 3. Push the Frame back down (70px) and to the right (Column 1)
            MainFrame.Margin = new Thickness(0, 70, 0, 0);
            System.Windows.Controls.Grid.SetColumn(MainFrame, 1);
            System.Windows.Controls.Grid.SetColumnSpan(MainFrame, 1);
        }

        public void ShowFullScreenPage()
        {
            // 1. Hide the Top Bar and Side Menu
            TopBarGrid.Visibility = Visibility.Collapsed;
            SideMenu.Visibility = Visibility.Collapsed;

            // 2. Collapse the Side Menu Column
            MainLayout.ColumnDefinitions[0].Width = new GridLength(0);

            // 3. Make the Frame fill the entire window (Margin 0, Span 2 columns)
            MainFrame.Margin = new Thickness(0); // Removes the 70px top gap
            System.Windows.Controls.Grid.SetColumn(MainFrame, 0); // Start from the far left
            System.Windows.Controls.Grid.SetColumnSpan(MainFrame, 2); // Span the whole width
                                                                      // Setting ResizeMode to NoResize in a maximized Borderless window covers the taskbar
            this.ResizeMode = ResizeMode.NoResize;

            // We toggle WindowState to refresh the layout immediately
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.WindowState = WindowState.Maximized;
            }
        }

        public void RestoreHomeLayout()
        {
            // 1. IMPORTANT: Make the Top Bar visible again
            TopBarGrid.Visibility = Visibility.Visible;

            // 2. Make the Side Menu visible
            SideMenu.Visibility = Visibility.Visible;

            // 3. Reset the Column Width (bring back the menu space)
            MainLayout.ColumnDefinitions[0].Width = new GridLength(400);

            // 4. Reset the Frame Margin (push it down 70px so it doesn't overlap the top bar)
            MainFrame.Margin = new Thickness(0, 70, 0, 0);

            // 5. Reset Column Spanning (Frame goes back to Column 1)
            System.Windows.Controls.Grid.SetColumn(MainFrame, 1);
            System.Windows.Controls.Grid.SetColumnSpan(MainFrame, 1);

            // 6. Clear the frame content (Optional: removes the page)
            MainFrame.Content = null;
            // Setting ResizeMode back to CanResize allows the taskbar to show again
            this.ResizeMode = ResizeMode.CanResize;

            // Toggle WindowState to refresh
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.WindowState = WindowState.Maximized;
            }
        }



        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
              ? WindowState.Normal
              : WindowState.Maximized;
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                Maximize_Click(sender, e);
            else
                DragMove();
        }


        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Topmost = true;
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Topmost = false;   // allows taskbar
        }



    }
}
