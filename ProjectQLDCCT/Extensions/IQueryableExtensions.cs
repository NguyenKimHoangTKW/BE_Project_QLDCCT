using System.Linq.Expressions;

public static class IQueryableExtensions
{
    public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> source, string propertyName, bool ascending = true)
    {
        var type = typeof(T);
        var property = type.GetProperty(propertyName,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        if (property == null)
            throw new ArgumentException($"Property '{propertyName}' not found on type '{type.Name}'");

        var parameter = Expression.Parameter(type, "x");
        var selector = Expression.Lambda(Expression.Property(parameter, property), parameter);

        var method = ascending ? "OrderBy" : "OrderByDescending";

        var resultExp = Expression.Call(typeof(Queryable), method,
            new Type[] { type, property.PropertyType },
            source.Expression, Expression.Quote(selector));

        return source.Provider.CreateQuery<T>(resultExp);
    }
}
