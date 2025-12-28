using ProjectBase.Core.Pagination;
using System.Linq.Expressions;
using System.Reflection;

namespace ProjectBase.Core.Helpers;

public static class SortQueryHelper
{
    public static Func<IQueryable<T>, IOrderedQueryable<T>> Sort<T>(
        this Func<IQueryable<T>, IOrderedQueryable<T>> source,
        PaginationFilterQuery? query,
        Func<IQueryable<T>, IOrderedQueryable<T>>? defaultSort = null)
    {
        if (query?.Sort == null)
            return defaultSort ?? source;

        var field = query.Sort.Field;
        var methodName = query.Sort.Sort.GetSortMethod();

        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(methodName))
            return defaultSort ?? source;

        if (!HasPropertyPath(typeof(T), field))
            return defaultSort ?? source;

        return order => order.DynamicOrder(field, methodName);
    }

    private static IOrderedQueryable<T> DynamicOrder<T>(this IQueryable<T> source, string property, string methodName)
    {
        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(methodName))
            return (source as IOrderedQueryable<T>) ?? source.OrderBy(_ => 0);

        string[] props = property.Split('.');

        Type type = typeof(T);

        ParameterExpression arg = Expression.Parameter(type, "x");

        Expression expr = arg;
        foreach (string prop in props)
        {
            PropertyInfo? pi = type.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi == null)
                return (source as IOrderedQueryable<T>) ?? source.OrderBy(_ => 0);

            expr = Expression.Property(expr, pi);
            type = pi.PropertyType;
        }

        Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);

        LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);

        object? resultObj = typeof(Queryable).GetMethods().Single(
                method => method.Name == methodName
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), type)
                .Invoke(null, new object[] { source, lambda });

        return resultObj as IOrderedQueryable<T> ?? source.OrderBy(_ => 0);
    }

    private static bool HasPropertyPath(Type type, string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath)) return false;

        foreach (var segment in propertyPath.Split('.'))
        {
            var pi = type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi == null) return false;
            type = pi.PropertyType;
        }

        return true;
    }
}