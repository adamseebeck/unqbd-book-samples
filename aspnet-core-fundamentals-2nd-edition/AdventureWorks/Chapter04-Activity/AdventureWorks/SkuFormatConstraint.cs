namespace AdventureWorks;

public sealed class SkuFormatConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route,
        string routeKey, RouteValueDictionary values,
        RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) &&
        value is string sku && IsSku(sku);

    // Three ASCII uppercase letters, a hyphen, four ASCII digits.
    static bool IsSku(string sku) =>
        sku.Length == 8 &&
        sku.Take(3).All(char.IsAsciiLetterUpper) &&
        sku[3] == '-' &&
        sku.Skip(4).All(char.IsAsciiDigit);
}
