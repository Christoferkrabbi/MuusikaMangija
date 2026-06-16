using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using MuusikaMangija.Services;

namespace MuusikaMangija.Converters;

public class BoolToPlayPauseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var playText = LocalizationManager.Instance["Play"] ?? "Play";
        var pauseText = LocalizationManager.Instance["Pause"] ?? "Pause";

        if (value is bool b)
            return b ? pauseText : playText;
        return playText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            var pauseText = LocalizationManager.Instance["Pause"] ?? "Pause";
            return s.Equals(pauseText, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
