using System.Text.RegularExpressions;

namespace WarehouseApi.RoutingConstraints;

public partial class SkuRouteConstraint : IRouteConstraint
{
    // The source generator writes the matching code at build time.
    // [0-9] accepts ASCII digits only, unlike \d, and \z ends the
    // value, because $ also matches before a final newline
    [GeneratedRegex(@"^[A-Z]{2,4}-[0-9]{3,8}\z")]
    private static partial Regex SkuPattern();

    // The POST handler checks a SKU from the request body with the same pattern
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
