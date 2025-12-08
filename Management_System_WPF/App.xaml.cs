using System.Windows;
using OfficeOpenXml;

namespace Management_System_WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            base.OnStartup(e);
        }
    }
}
