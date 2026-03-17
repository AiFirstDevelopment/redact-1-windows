using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Redact1.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && !string.IsNullOrEmpty(value.ToString())
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString()?.ToLower();
            return status switch
            {
                "new" => "#3182CE",
                "in_progress" => "#D69E2E",
                "completed" => "#38A169",
                "pending" => "#718096",
                "approved" => "#38A169",
                "rejected" => "#E53E3E",
                _ => "#718096"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString()?.ToLower();
            return status switch
            {
                "new" => "#EBF8FF",
                "in_progress" => "#FFFAF0",
                "completed" => "#F0FFF4",
                "pending" => "#F7FAFC",
                "approved" => "#F0FFF4",
                "rejected" => "#FED7D7",
                _ => "#F7FAFC"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString()?.ToLower();
            return status switch
            {
                "new" => "New",
                "in_progress" => "In Progress",
                "completed" => "Completed",
                "pending" => "Pending",
                "approved" => "Approved",
                "rejected" => "Rejected",
                "uploaded" => "Uploaded",
                "processing" => "Processing",
                "detected" => "Detected",
                "reviewed" => "Reviewed",
                "exported" => "Exported",
                _ => status ?? "Unknown"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
