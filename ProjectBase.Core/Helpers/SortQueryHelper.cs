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

        string[] props = property.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (props.Length == 0)
            return (source as IOrderedQueryable<T>) ?? source.OrderBy(_ => 0);

        var arg = Expression.Parameter(typeof(T), "x");
        var keyExpr = BuildSortKey(arg, props, index: 0, isDescending: methodName == nameof(Queryable.OrderByDescending), depth: 0);
        if (keyExpr == null)
            return (source as IOrderedQueryable<T>) ?? source.OrderBy(_ => 0);

        Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), keyExpr.Type);
        LambdaExpression lambda = Expression.Lambda(delegateType, keyExpr, arg);

        object? resultObj = typeof(Queryable).GetMethods().Single(
                method => method.Name == methodName
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), keyExpr.Type)
                .Invoke(null, new object[] { source, lambda });

        return resultObj as IOrderedQueryable<T> ?? source.OrderBy(_ => 0);
    }

    private static IOrderedQueryable<T> DynamicThenOrder<T>(this IOrderedQueryable<T> source, string property, string methodName)
    {
        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(methodName))
            return source;

        string[] props = property.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (props.Length == 0) return source;

        var arg = Expression.Parameter(typeof(T), "x");
        var keyExpr = BuildSortKey(arg, props, index: 0, isDescending: methodName == nameof(Queryable.ThenByDescending), depth: 0);
        if (keyExpr == null) return source;

        Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), keyExpr.Type);
        LambdaExpression lambda = Expression.Lambda(delegateType, keyExpr, arg);

        object? resultObj = typeof(Queryable).GetMethods().Single(
                method => method.Name == methodName
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), keyExpr.Type)
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

    private static Expression? BuildSortKey(Expression instance, string[] segments, int index, bool isDescending, int depth)
    {
        if (depth > 32) return null;
        if (index < 0 || index >= segments.Length) return null;

        var member = TryMemberAccess(instance, segments[index]);
        if (member == null) return null;

        // Collection segment (at any depth)
        if (IsEnumerableButNotString(member.Type))
        {
            // Collection.Count (must be terminal)
            if (index + 1 < segments.Length && string.Equals(segments[index + 1], "Count", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 2 != segments.Length) return null;
                return BuildCount(member);
            }

            // Need inner member path to aggregate
            if (index == segments.Length - 1) return null;

            var elementType = GetEnumerableElementType(member.Type);
            if (elementType == null) return null;

            var m = Expression.Parameter(elementType, "m");
            var innerKey = BuildSortKey(m, segments, index + 1, isDescending, depth + 1);
            if (innerKey == null) return null;

            var selectCall = CallEnumerableSelect(member, m, innerKey);
            var defaultIfEmpty = CallEnumerableDefaultIfEmpty(selectCall);
            return isDescending
                ? CallEnumerableMax(defaultIfEmpty)
                : CallEnumerableMin(defaultIfEmpty);
        }

        // Reference/value segment
        if (index == segments.Length - 1)
            return member;

        return BuildSortKey(member, segments, index + 1, isDescending, depth + 1);
    }

    private static Expression? TryMemberAccess(Expression instance, string memberName)
    {
        var type = instance.Type;
        var pi = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (pi != null) return Expression.Property(instance, pi);

        var fi = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (fi != null) return Expression.Field(instance, fi);

        return null;
    }

    private static bool IsEnumerableButNotString(Type type)
    {
        if (type == typeof(string)) return false;
        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    private static Type? GetEnumerableElementType(Type enumerableType)
    {
        if (enumerableType.IsArray) return enumerableType.GetElementType();

        if (enumerableType.IsGenericType)
        {
            var def = enumerableType.GetGenericTypeDefinition();
            if (def == typeof(IEnumerable<>) || def == typeof(ICollection<>) || def == typeof(IList<>) || def == typeof(List<>))
                return enumerableType.GetGenericArguments()[0];
        }

        var iface = enumerableType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return iface?.GetGenericArguments()[0];
    }

    private static Expression BuildCount(Expression enumerableMember)
    {
        var elementType = GetEnumerableElementType(enumerableMember.Type);
        if (elementType == null)
            return Expression.Constant(0);

        return Expression.Call(typeof(Enumerable), "Count", new[] { elementType }, enumerableMember);
    }

    private static Expression CallEnumerableSelect(Expression enumerableMember, ParameterExpression elementParam, Expression selectorBody)
    {
        var elementType = elementParam.Type;
        var keyType = selectorBody.Type;
        var lambda = Expression.Lambda(selectorBody, elementParam);
        return Expression.Call(typeof(Enumerable), "Select", new[] { elementType, keyType }, enumerableMember, lambda);
    }

    private static Expression CallEnumerableDefaultIfEmpty(Expression sequence)
    {
        return Expression.Call(typeof(Enumerable), "DefaultIfEmpty", new[] { sequence.Type.GetGenericArguments()[0] }, sequence);
    }

    private static Expression CallEnumerableMin(Expression sequence)
    {
        var keyType = sequence.Type.GetGenericArguments()[0];
        return Expression.Call(typeof(Enumerable), "Min", new[] { keyType }, sequence);
    }

    private static Expression CallEnumerableMax(Expression sequence)
    {
        var keyType = sequence.Type.GetGenericArguments()[0];
        return Expression.Call(typeof(Enumerable), "Max", new[] { keyType }, sequence);
    }

    private static string? MapField(IReadOnlyDictionary<string, string>? fieldMap, string? field)
    {
        if (string.IsNullOrWhiteSpace(field)) return field;
        if (fieldMap == null) return field;
        return fieldMap.TryGetValue(field, out var mapped) && !string.IsNullOrWhiteSpace(mapped) ? mapped : field;
    }
}