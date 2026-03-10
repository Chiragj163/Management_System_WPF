using System.Windows;
using System.Windows.Input;

namespace Management_System_WPF
{
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
            // Pre-fill current username for convenience
            txtNewUser.Text = Properties.Settings.Default.AppUsername;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string currentSavedPass = Properties.Settings.Default.AppPassword;

            // 1. Verify Old Password
            if (txtOldPass.Password != currentSavedPass)
            {
                MessageBox.Show("Current password is incorrect.", "Security Check", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validate New Inputs
            if (string.IsNullOrWhiteSpace(txtNewUser.Text) || string.IsNullOrWhiteSpace(txtNewPass.Password))
            {
                MessageBox.Show("Username and Password cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Save New Credentials
            Properties.Settings.Default.AppUsername = txtNewUser.Text;
            Properties.Settings.Default.AppPassword = txtNewPass.Password;
            Properties.Settings.Default.Save(); // Persist changes

            MessageBox.Show("Credentials updated successfully! Please login with new details.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}