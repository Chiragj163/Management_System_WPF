using System;
using System.Globalization;
using System.Windows.Data;

namespace Management_System_WPF.Helpers
{
    public class DateOrTotalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                if (date == DateTime.MinValue)
                    return "Total";

                return date.ToString("dd-MM-yyyy");
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}