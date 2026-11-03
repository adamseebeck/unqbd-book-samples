using AdventureWorks.Core;

namespace AdventureWorks;

public sealed class ProductCategoryConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route,
        string routeKey, RouteValueDictionary values,
        RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value) &&
        value is string category &&
        ProductCategories.All.Contains(category);
}
