using System.Text.RegularExpressions;

namespace WarehouseApi.RoutingConstraints;

public partial class WarehouseCodeConstraint : IRouteConstraint
{
    // Warehouse codes compare without regard to case, so wh-001 matches as well as WH-001
    [GeneratedRegex(@"^WH-[0-9]{3}\z", RegexOptions.IgnoreCase)]
    private static partial Regex WarehouseCodePattern();

    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        return values.TryGetValue(routeKey, out var value)
            && value is string code
            && WarehouseCodePattern().IsMatch(code);
    }
}
