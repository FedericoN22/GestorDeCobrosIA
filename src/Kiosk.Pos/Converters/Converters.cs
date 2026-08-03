using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Kiosk.Pos.Converters;

public sealed class CentavosArsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int centavos)
        {
            return (centavos / 100m).ToString("C", new CultureInfo("es-AR"));
        }

        return "$0,00";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StockBajoConverter : IValueConverter
{
    private static readonly Brush SinStock = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
    private static readonly Brush Bajo = new SolidColorBrush(Color.FromRgb(0xB5, 0x47, 0x08));
    private static readonly Brush Normal = new SolidColorBrush(Color.FromRgb(0x34, 0x40, 0x54));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int stock)
        {
            return stock <= 0 ? SinStock : stock <= 5 ? Bajo : Normal;
        }

        return Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
