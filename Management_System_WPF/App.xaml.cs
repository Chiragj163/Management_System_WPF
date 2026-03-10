using System.Windows;
using OfficeOpenXml;
using Management_System_WPF.Views;

namespace Management_System_WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            base.OnStartup(e);
            LoginWindow login = new LoginWindow();
            bool? result = login.ShowDialog();
            if (result == true)
            {
             
                MainWindow main = new MainWindow();

             
                this.MainWindow = main;

                main.Show();
            }
            else
            {
               
                this.Shutdown();
            }
        }
    }
}
