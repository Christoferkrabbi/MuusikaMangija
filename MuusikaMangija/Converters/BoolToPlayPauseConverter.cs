using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MuusikaMangija.Converters;

public class BoolToPlayPauseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? "Pause" : "Play";
        return "Play";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
            return s.Equals("Pause", StringComparison.OrdinalIgnoreCase);
        return false;
    }
}
