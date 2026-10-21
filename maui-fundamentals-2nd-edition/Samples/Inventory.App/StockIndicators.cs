using MauiInventory.Core;
using System.Globalization;

namespace MauiInventory.App;
public sealed class StockLevelToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is int amount ? amount < 10 ? Colors.Red : amount <= 50 ? Colors.Orange : Colors.Green : BindableProperty.UnsetValue;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class StockTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is int amount ? amount < 10 ? "Low" : amount <= 50 ? "Moderate" : "Healthy" : "Unknown stock";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
