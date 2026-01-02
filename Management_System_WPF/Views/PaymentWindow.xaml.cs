using System.Windows;

namespace Management_System_WPF.Views
{
    public partial class PaymentWindow : Window
    {
        public decimal Amount { get; private set; }

        public PaymentWindow(decimal currentAmount)
        {
            InitializeComponent();
            txtAmount.Text = currentAmount == 0 ? "" : currentAmount.ToString("0.##");
            txtAmount.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAmount.Text)) txtAmount.Text = "0";

            if (decimal.TryParse(txtAmount.Text, out decimal val))
            {
                Amount = val;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid number.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}