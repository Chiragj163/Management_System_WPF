using System.Windows;
using System.Windows.Input;

namespace Management_System_WPF.Views 
{
    public partial class PasswordConfirmWindow : Window
    {
        public PasswordConfirmWindow()
        {
            InitializeComponent();
            txtPass.Focus(); 
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string currentPass = Properties.Settings.Default.AppPassword;

            if (txtPass.Password == currentPass)
            {
                this.DialogResult = true;
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