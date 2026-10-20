using Microsoft.AspNetCore.Routing;

public sealed class ProductCategoryConstraint : IRouteConstraint
{
    private static readonly HashSet<string> Categories = new(StringComparer.OrdinalIgnoreCase)
        { "electronics", "clothing", "home", "sports" };

    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) && value is string category &&
        Categories.Contains(category);
}
