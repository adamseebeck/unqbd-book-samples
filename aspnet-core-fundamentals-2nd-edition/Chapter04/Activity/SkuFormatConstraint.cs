public sealed class SkuFormatConstraint : IRouteConstraint
{
    public bool Match(HttpContext? context, IRouter? route, string key, RouteValueDictionary values, RouteDirection direction)
    {
        if (!values.TryGetValue(key, out var raw) || raw is not string sku || sku.Length != 8) return false;
        return sku.Take(3).All(char.IsAsciiLetterUpper) && sku[3] == '-' && sku.Skip(4).All(char.IsAsciiDigit);
    }
}
