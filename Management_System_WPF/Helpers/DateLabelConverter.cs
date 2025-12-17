using System;
using System.Globalization;
using System.Windows.Data;

namespace Management_System_WPF.Helpers
{
    public class DateLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "";

            // Handle TOTAL / TEXT rows
            if (value is string str && !DateTime.TryParse(str, out _))
                return str;

            DateTime date;

            if (value is DateTime dt)
                date = dt;
            else if (!DateTime.TryParse(value.ToString(), out date))
                return value.ToString();

            // 🔥 FORCE dd/MM/yyyy
            return date.ToString("dd/MM/yyyy");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
