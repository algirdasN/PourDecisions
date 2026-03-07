using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PourDecisions.Application.AvailabilityEngine;

namespace PourDecisions.Desktop.Converters;

public class AvailabilityStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is AvailabilityStatus status
            ? status switch
            {
                AvailabilityStatus.Available => Brushes.ForestGreen,
                AvailabilityStatus.Unavailable => Brushes.OrangeRed,
                _ => null
            }
            : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
