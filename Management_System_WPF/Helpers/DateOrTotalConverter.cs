using System;
using System.Globalization;
using System.Windows.Data;

namespace Management_System_WPF.Helpers
{
    public class DateOrTotalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                return dt == DateTime.MinValue
                    ? "Total"
                    : dt.ToString("dd/MM/yyyy");
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
