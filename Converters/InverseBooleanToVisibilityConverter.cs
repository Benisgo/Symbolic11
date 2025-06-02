using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Symbolic11.Converters // Make sure this namespace matches your project structure
{
    // Converts bool to Visibility: true -> Collapsed, false -> Visible
    public class InverseBooleanToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue) {
                // The logic is inverted
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible; // Default or handle non-boolean input
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            // One-way binding, so ConvertBack is not needed.
            throw new NotImplementedException();
        }
    }
}