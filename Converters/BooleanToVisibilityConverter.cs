using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Symbolic11.Converters // Make sure this namespace matches your project structure
{
    // Converts bool to Visibility: true -> Visible, false -> Collapsed
    public class BooleanToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue) {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed; // Default or handle non-boolean input
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            // One-way binding, so ConvertBack is not needed.
            throw new NotImplementedException();
        }
    }
}