using System.Text.RegularExpressions;

namespace WarehouseApi.RoutingConstraints;

public partial class SkuRouteConstraint : IRouteConstraint
{
    // The regular expression source generator writes the matching code at build time
    [GeneratedRegex(@"^[A-Z]{2,4}-[0-9]{3,8}\z")]
    private static partial Regex SkuPattern();

    // [ValidSku] checks SKUs in request bodies with this method, so the format is defined once
    public static bool IsValidSku(string? sku) => sku is not null && SkuPattern().IsMatch(sku);

    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        return values.TryGetValue(routeKey, out var value) && IsValidSku(value as string);
    }
}
