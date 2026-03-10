using Management_System_WPF.Views; // Ensure you have this if ChangePasswordWindow is in Views
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

            // ✅ FIX 1: Pre-fill Username from Settings
            txtUser.Text = Properties.Settings.Default.AppUsername;

            // ✅ FIX 2: Make it Read-Only (User cannot edit)
            txtUser.IsReadOnly = true;
            txtUser.Focusable = false; // Skips tab stop
            txtUser.Background = Brushes.WhiteSmoke; // Visual cue that it's locked
            txtUser.Foreground = Brushes.Gray;

            // ✅ FIX 3: Set Focus directly to Password box
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
            // We only need to check the password, since username is fixed from settings
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
                // ✅ FIX 4: Refresh the Fixed Username if it was changed
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