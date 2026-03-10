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
        private Action _refreshParent; 
        private int _buyerId;
        private int? _editingPaymentId = null; 

        public PaymentWindow(int buyerId, List<PaymentRecord> history, Action onRefreshParent)
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            _buyerId = buyerId;
            _refreshParent = onRefreshParent;

            _paymentHistory = new ObservableCollection<PaymentRecord>(history ?? new List<PaymentRecord>());
            dgHistory.ItemsSource = _paymentHistory;

            ResetInput();
        }
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
                    PaymentService.AddPayment(_buyerId, date, amount);
                    var newRecord = new PaymentRecord { Id = 0, Date = date, Amount = amount };
                    _paymentHistory.Insert(0, newRecord);
                    RefreshHistoryFromDB();
                }
                else
                {
                    PaymentService.UpdatePayment(_editingPaymentId.Value, date, amount);
                    var item = _paymentHistory.FirstOrDefault(x => x.Id == _editingPaymentId.Value);
                    if (item != null)
                    {
                        item.Amount = amount;
                        item.Date = date;
                        dgHistory.Items.Refresh(); 
                    }

                    ResetInput();
                }

                _refreshParent?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var paymentRecord = button.DataContext as PaymentRecord;

            if (paymentRecord != null)
            {
                txtAmount.Text = paymentRecord.Amount.ToString("0.##");
                dpPaymentDate.SelectedDate = paymentRecord.Date;

                _editingPaymentId = paymentRecord.Id;
                btnSave.Content = "Update Payment"; 
                txtAmount.Focus();
            }
        }

       
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
                        _paymentHistory.Remove(paymentRecord);
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
                ResetInput();
            }
            else
            {
                Close();
            }
        }

        private void ResetInput()
        {
            txtAmount.Text = "";
            dpPaymentDate.SelectedDate = DateTime.Today;
            _editingPaymentId = null;
            btnSave.Content = "Save Payment"; 
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