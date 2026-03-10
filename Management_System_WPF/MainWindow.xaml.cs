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
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shell;
using System.Windows.Threading;
using System.IO;

namespace Management_System_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string savedPath = Properties.Settings.Default.BackgroundImagePath;

            if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
            {
                RootGrid.Background = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(savedPath)),
                    Stretch = Stretch.UniformToFill
                };
            }

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

        public void ResetLayoutBeforeNavigation()
        {
            TopBarGrid.Visibility = Visibility.Visible;
            SideMenu.Visibility = Visibility.Visible;
            MainLayout.ColumnDefinitions[0].Width = new GridLength(400);
            MainFrame.Margin = new Thickness(0, 70, 0, 0);
            System.Windows.Controls.Grid.SetColumn(MainFrame, 1);
            System.Windows.Controls.Grid.SetColumnSpan(MainFrame, 1);
        }

        public void ShowFullScreenPage()
        {
            TopBarGrid.Visibility = Visibility.Collapsed;
            SideMenu.Visibility = Visibility.Collapsed;
            MainLayout.ColumnDefinitions[0].Width = new GridLength(0);
            MainFrame.Margin = new Thickness(0); 
            System.Windows.Controls.Grid.SetColumn(MainFrame, 0); 
            System.Windows.Controls.Grid.SetColumnSpan(MainFrame, 2);
                                                                     
            this.ResizeMode = ResizeMode.NoResize;
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.WindowState = WindowState.Maximized;
            }
        }

        public void RestoreHomeLayout()
        {
            TopBarGrid.Visibility = Visibility.Visible;
            SideMenu.Visibility = Visibility.Visible;
            MainLayout.ColumnDefinitions[0].Width = new GridLength(400);
            MainFrame.Margin = new Thickness(0, 70, 0, 0);

            System.Windows.Controls.Grid.SetColumn(MainFrame, 1);
            System.Windows.Controls.Grid.SetColumnSpan(MainFrame, 1);
            MainFrame.Content = null;
            this.ResizeMode = ResizeMode.CanResize;
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

        private void ChangeBackground_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.FileName;

                // Change background
                RootGrid.Background = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(path)),
                    Stretch = Stretch.UniformToFill
                };
                Properties.Settings.Default.BackgroundImagePath = path;
                Properties.Settings.Default.Save();

            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            LoginWindow login = new LoginWindow();
            if (login.ShowDialog() == true)
            {
                this.Show();
                RestoreHomeLayout();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordWindow resetWin = new ChangePasswordWindow();
            resetWin.Owner = this;
            resetWin.ShowDialog();
        }
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }
    }
}
