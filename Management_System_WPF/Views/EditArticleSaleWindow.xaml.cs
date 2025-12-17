using System;
using System.Windows;
using Management_System_WPF.Services;

namespace Management_System_WPF.Views
{
    public partial class EditArticleSaleWindow : Window
    {
        private readonly string _article;
        private readonly DateTime _date;

        public EditArticleSaleWindow(string article, DateTime date)
        {
            InitializeComponent();

            _article = article;
            _date = date;

            txtArticle.Text = article;
            txtDate.Text = date.ToString("dd/MM/yyyy");

            txtQty.Text = SalesService
                .GetArticleQtyByDate(article, date)
                .ToString();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtQty.Text, out int qty))
            {
                MessageBox.Show("Invalid quantity");
                return;
            }

            SalesService.UpdateArticleSaleQty(_article, _date, qty);
            DialogResult = true;
            Close();
        }
    }
}
