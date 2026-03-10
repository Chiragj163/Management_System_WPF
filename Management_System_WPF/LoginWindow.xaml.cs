using Management_System_WPF.Views;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Management_System_WPF
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            LoadBackground();
            txtUser.Text = Properties.Settings.Default.AppUsername;
            txtUser.IsReadOnly = true;
            txtUser.Focusable = false; 
            txtUser.Background = Brushes.WhiteSmoke; 
            txtUser.Foreground = Brushes.Gray;
            txtPass.Focus();
        }

        private void LoadBackground()
        {
            string savedPath = Management_System_WPF.Properties.Settings.Default.BackgroundImagePath;
            if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
            {
                LoginBg.ImageSource = new BitmapImage(new Uri(savedPath));
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
           
            string savedPass = Properties.Settings.Default.AppPassword;

            if (txtPass.Password == savedPass)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid Password.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                txtPass.Clear();
                txtPass.Focus();
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordWindow resetWin = new ChangePasswordWindow();
            resetWin.Owner = this;

            if (resetWin.ShowDialog() == true)
            {
                txtUser.Text = Properties.Settings.Default.AppUsername;

                txtPass.Clear();
                txtPass.Focus();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
       
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login_Click(sender, e);
            }
        }
    }
}