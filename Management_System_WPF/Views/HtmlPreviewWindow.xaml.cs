using Microsoft.Web.WebView2.Core;
using System.Windows;

namespace Management_System_WPF.Views
{
    public partial class HtmlPreviewWindow : Window
    {
        public HtmlPreviewWindow(string html)
        {
            InitializeComponent();
            LoadHtml(html);
        }

        private async void LoadHtml(string html)
        {
            await webView.EnsureCoreWebView2Async();
            webView.NavigateToString(html);
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            await webView.ExecuteScriptAsync("window.print();");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}