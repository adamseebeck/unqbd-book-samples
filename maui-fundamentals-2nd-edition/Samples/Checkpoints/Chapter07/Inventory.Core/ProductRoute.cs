namespace MauiInventory.Core;
public static class ProductRoute
{
    public static string Detail(int id)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));
        return $"details?id={id}";
    }

    public static bool TryRead(object? value, out int id) => int.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out id) && id > 0;
}
