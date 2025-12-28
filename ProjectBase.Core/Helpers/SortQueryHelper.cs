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
        var sorts = query?.Sorts;
        if (sorts != null && sorts.Count > 0)
            return order => order.DynamicOrderChain(sorts);

        return defaultSort ?? source;
    }

    /// <summary>
    /// Sort with an external field key mapped to an internal entity member path (allow-list).
    /// Example map: "customerName" => "Customer.Name"
    /// </summary>
    public static Func<IQueryable<T>, IOrderedQueryable<T>> Sort<T>(
        this Func<IQueryable<T>, IOrderedQueryable<T>> source,
        PaginationFilterQuery? query,
        IReadOnlyDictionary<string, string>? fieldMap,
        Func<IQueryable<T>, IOrderedQueryable<T>>? defaultSort = null)
    {
        var sorts = query?.Sorts;
        if (sorts != null && sorts.Count > 0)
        {
            var mappedSorts = sorts
                .Select(s => new SortQuery
                {
                    Field = MapField(fieldMap, s.Field),
                    Sort = s.Sort
                })
                .ToList();

            return order => order.DynamicOrderChain(mappedSorts);
        }

        return defaultSort ?? source;
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

    private static IOrderedQueryable<T> DynamicThenOrder<T>(this IOrderedQueryable<T> source, string property, string methodName)
    {
        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(methodName))
            return source;

        string[] props = property.Split('.');

        Type type = typeof(T);

        ParameterExpression arg = Expression.Parameter(type, "x");

        Expression expr = arg;
        foreach (string prop in props)
        {
            PropertyInfo? pi = type.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi == null)
                return source;

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

        return resultObj as IOrderedQueryable<T> ?? source;
    }

    private static IOrderedQueryable<T> DynamicOrderChain<T>(this IQueryable<T> source, IReadOnlyList<SortQuery> sorts)
    {
        IOrderedQueryable<T>? ordered = null;

        foreach (var s in sorts)
        {
            var field = s.Field;
            var methodName = s.Sort.GetSortMethod();

            if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(methodName))
                continue;

            if (!HasPropertyPath(typeof(T), field))
                continue;

            if (ordered == null)
            {
                ordered = source.DynamicOrder(field, methodName);
            }
            else
            {
                var thenMethod = methodName == nameof(Queryable.OrderBy)
                    ? nameof(Queryable.ThenBy)
                    : methodName == nameof(Queryable.OrderByDescending)
                        ? nameof(Queryable.ThenByDescending)
                        : methodName;

                ordered = ordered.DynamicThenOrder(field, thenMethod);
            }
        }

        return ordered ?? source.OrderBy(_ => 0);
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

    private static string? MapField(IReadOnlyDictionary<string, string>? fieldMap, string? field)
    {
        if (string.IsNullOrWhiteSpace(field)) return field;
        if (fieldMap == null) return field;
        return fieldMap.TryGetValue(field, out var mapped) && !string.IsNullOrWhiteSpace(mapped) ? mapped : field;
    }
}