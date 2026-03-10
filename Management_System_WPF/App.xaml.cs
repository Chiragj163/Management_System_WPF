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
                // 3. Login Successful -> Open Main Window
                MainWindow main = new MainWindow();

                // Tell the App this is now the main window
                this.MainWindow = main;

                main.Show();
            }
            else
            {
                // 4. Login Failed/Cancelled -> Kill the App
                this.Shutdown();
            }
        }
    }
}
