using System.Globalization;
using Avalonia.Data.Converters;

namespace PourDecisions.Desktop.Converters;

public class BoolToFavoriteLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue
            ? boolValue switch
            {
                true => "⭐️ ",
                false => string.Empty
            }
            : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
