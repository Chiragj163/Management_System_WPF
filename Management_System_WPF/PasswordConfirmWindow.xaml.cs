using System.Windows;
using System.Windows.Input; // ✅ Required for dragging

namespace Management_System_WPF.Views // ✅ Ensure namespace matches your file structure
{
    public partial class PasswordConfirmWindow : Window
    {
        public PasswordConfirmWindow()
        {
            InitializeComponent();
            txtPass.Focus(); // Focus password box immediately
        }

        // ✅ NEW: Allows dragging the borderless window
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            // Verify against the stored password
            string currentPass = Properties.Settings.Default.AppPassword;

            if (txtPass.Password == currentPass)
            {
                this.DialogResult = true; // Success
                this.Close();
            }
            else
            {
                MessageBox.Show("Incorrect Password.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                txtPass.Clear();
                txtPass.Focus();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}