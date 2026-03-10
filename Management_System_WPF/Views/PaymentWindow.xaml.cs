using Management_System_WPF.Models;
using Management_System_WPF.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Management_System_WPF.Views
{
    public partial class PaymentWindow : Window
    {
        private ObservableCollection<PaymentRecord> _paymentHistory;
        private Action _refreshParent; // Delegate to tell parent to refresh totals
        private int _buyerId;

        // Variables to handle "Edit Mode"
        private int? _editingPaymentId = null; // Stores ID if we are editing

        public PaymentWindow(int buyerId, List<PaymentRecord> history, Action onRefreshParent)
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            _buyerId = buyerId;
            _refreshParent = onRefreshParent;

            // Setup List
            _paymentHistory = new ObservableCollection<PaymentRecord>(history ?? new List<PaymentRecord>());
            dgHistory.ItemsSource = _paymentHistory;

            // Defaults
            ResetInput();
        }

        // 1. SAVE / UPDATE BUTTON
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAmount.Text)) txtAmount.Text = "0";
            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount.");
                return;
            }
            if (!dpPaymentDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a date.");
                return;
            }

            DateTime date = dpPaymentDate.SelectedDate.Value;

            try
            {
                if (_editingPaymentId == null)
                {
                    // --- CREATE NEW ---
                    PaymentService.AddPayment(_buyerId, date, amount);

                    // Add to UI List (We can fetch the ID, but for UI feedback just adding is enough)
                    // Ideally, reload the list from DB to get the new ID, but for speed:
                    var newRecord = new PaymentRecord { Id = 0, Date = date, Amount = amount };
                    _paymentHistory.Insert(0, newRecord);

                    // Reload list completely to get the correct ID (important for future edits)
                    RefreshHistoryFromDB();
                }
                else
                {
                    // --- UPDATE EXISTING ---
                    PaymentService.UpdatePayment(_editingPaymentId.Value, date, amount);

                    // Update UI List
                    var item = _paymentHistory.FirstOrDefault(x => x.Id == _editingPaymentId.Value);
                    if (item != null)
                    {
                        item.Amount = amount;
                        item.Date = date;
                        dgHistory.Items.Refresh(); // Force grid refresh
                    }

                    // Reset Mode
                    ResetInput();
                }

                // Notify Parent Page to recalculate "Grand Total"
                _refreshParent?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // 2. EDIT ROW
        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            // Get the button that was clicked
            var button = sender as Button;
            var paymentRecord = button.DataContext as PaymentRecord;

            if (paymentRecord != null)
            {
                // Fill Inputs
                txtAmount.Text = paymentRecord.Amount.ToString("0.##");
                dpPaymentDate.SelectedDate = paymentRecord.Date;

                // Enter Edit Mode
                _editingPaymentId = paymentRecord.Id;
                btnSave.Content = "Update Payment"; // Change Button Text
                txtAmount.Focus();
            }
        }

        // 3. DELETE ROW
        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var paymentRecord = button.DataContext as PaymentRecord;

            if (paymentRecord != null)
            {
                if (MessageBox.Show($"Delete payment of {paymentRecord.Amount}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        PaymentService.DeletePayment(paymentRecord.Id);

                        // Remove from UI
                        _paymentHistory.Remove(paymentRecord);

                        // Notify Parent
                        _refreshParent?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting: " + ex.Message);
                    }
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_editingPaymentId != null)
            {
                // If editing, "Cancel" just clears the edit mode
                ResetInput();
            }
            else
            {
                // If adding, "Cancel" closes window
                Close();
            }
        }

        private void ResetInput()
        {
            txtAmount.Text = "";
            dpPaymentDate.SelectedDate = DateTime.Today;
            _editingPaymentId = null;
            btnSave.Content = "Save Payment"; // Reset Button Text
            txtAmount.Focus();
        }

        private void RefreshHistoryFromDB()
        {
           
            var newList = PaymentService.GetPaymentsList(_buyerId, dpPaymentDate.SelectedDate.Value.Year, dpPaymentDate.SelectedDate.Value.Month);
            _paymentHistory.Clear();
            foreach (var item in newList) _paymentHistory.Add(item);
        }
    }
}