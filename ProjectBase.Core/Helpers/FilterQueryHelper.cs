using ProjectBase.Core.Entities;
using ProjectBase.Core.Pagination;
using System.Linq.Expressions;
using System.Reflection;

namespace ProjectBase.Core.Helpers;

public static class FilterQueryHelper
{
    public static Expression<Func<T, bool>> Filter<T>(this Expression<Func<T, bool>> predicate, PaginationFilterQuery query) where T : BaseEntity
    {
        if (query == null) return predicate;

        // Grouped filters (parentheses) take precedence
        if (query.FilterGroup != null)
        {
            var paramExp = predicate.Parameters[0];
            var grouped = BuildGroupCondition<T>(paramExp, query.FilterGroup, depth: 0);
            if (grouped == null) return predicate;

            var groupedBody = Expression.AndAlso(predicate.Body, grouped);
            return Expression.Lambda<Func<T, bool>>(groupedBody, paramExp);
        }

        var filters = NormalizeFilters(query);
        if (filters.Count == 0) return predicate;

        var logic = (query.FilterLogic ?? "and").Trim();
        var isOr = string.Equals(logic, "or", StringComparison.OrdinalIgnoreCase);

        var parameter = predicate.Parameters[0];
        Expression? combined = null;

        foreach (var f in filters)
        {
            var condition = BuildCondition<T>(parameter, f);
            if (condition == null) continue;

            combined = combined == null
                ? condition
                : isOr ? Expression.OrElse(combined, condition) : Expression.AndAlso(combined, condition);
        }

        if (combined == null) return predicate;

        var body = Expression.AndAlso(predicate.Body, combined);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Filter with an external field key mapped to an internal entity member path (allow-list).
    /// Example map: "customerName" => "Customer.Name"
    /// </summary>
    public static Expression<Func<T, bool>> Filter<T>(
        this Expression<Func<T, bool>> predicate,
        PaginationFilterQuery query,
        IReadOnlyDictionary<string, string>? fieldMap) where T : BaseEntity
    {
        if (query == null) return predicate;

        // map without mutating the incoming query object (important for reuse/logging)
        var mappedQuery = new PaginationFilterQuery(query.PageNumber, query.PageSize)
        {
            FilterLogic = query.FilterLogic,
            Sorts = query.Sorts,
            FilterItems = query.FilterItems?.Select(f => MapFilter(f, fieldMap)).ToList(),
            FilterGroup = query.FilterGroup == null ? null : MapGroup(query.FilterGroup, fieldMap, depth: 0)
        };

        return predicate.Filter(mappedQuery);
    }

    private static List<FilterColumnQuery> NormalizeFilters(PaginationFilterQuery query)
    {
        if (query.FilterItems != null && query.FilterItems.Count > 0)
            return query.FilterItems.Where(f => f != null).ToList();

        return new List<FilterColumnQuery>();
    }

    private static FilterColumnQuery MapFilter(FilterColumnQuery filter, IReadOnlyDictionary<string, string>? fieldMap)
    {
        var field = filter.ColumnField;
        if (!string.IsNullOrWhiteSpace(field) && fieldMap != null && fieldMap.TryGetValue(field, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
            field = mapped;

        return new FilterColumnQuery
        {
            ColumnField = field,
            OperatorValue = filter.OperatorValue,
            Value = filter.Value
        };
    }

    private static FilterGroupQuery MapGroup(FilterGroupQuery group, IReadOnlyDictionary<string, string>? fieldMap, int depth)
    {
        if (depth > 32) return new FilterGroupQuery { Logic = group.Logic };

        return new FilterGroupQuery
        {
            Negate = group.Negate,
            Logic = group.Logic,
            Items = group.Items?.Select(i => MapFilter(i, fieldMap)).ToList(),
            Groups = group.Groups?.Select(g => MapGroup(g, fieldMap, depth + 1)).ToList()
        };
    }

    private static Expression? BuildCondition<T>(ParameterExpression parameter, FilterColumnQuery filter) where T : BaseEntity
    {
        if (filter == null) return null;
        if (string.IsNullOrWhiteSpace(filter.ColumnField)) return null;

        var field = filter.ColumnField!.Trim();

        // dot-path: navigation or collection Any
        if (field.IndexOf('.') > -1)
        {
            var segments = field.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length < 2) return null;

            var first = TryMemberAccess(parameter, segments[0]);
            if (first == null) return null;

            if (IsEnumerableButNotString(first.Type))
            {
                // Offers.Count
                if (segments.Length == 2 && string.Equals(segments[1], "Count", StringComparison.OrdinalIgnoreCase))
                    return BuildCountCondition(first, filter);

                return BuildAnyCondition(first, segments.Skip(1).ToArray(), filter);
            }

            Expression? member = first;
            foreach (var seg in segments.Skip(1))
            {
                member = TryMemberAccess(member, seg);
                if (member == null) return null;
            }

            return BuildMemberCondition(member, filter);
        }

        // single member on root
        var rootMember = TryMemberAccess(parameter, field);
        return rootMember == null ? null : BuildMemberCondition(rootMember, filter);
    }

    private static Expression? BuildGroupCondition<T>(ParameterExpression parameter, FilterGroupQuery group, int depth) where T : BaseEntity
    {
        if (depth > 32) return null; // hard guard against abuse

        var logic = (group.Logic ?? "and").Trim();
        var isOr = string.Equals(logic, "or", StringComparison.OrdinalIgnoreCase);

        Expression? combined = null;

        if (group.Items != null)
        {
            foreach (var item in group.Items)
            {
                var cond = BuildCondition<T>(parameter, item);
                if (cond == null) continue;

                combined = combined == null
                    ? cond
                    : isOr ? Expression.OrElse(combined, cond) : Expression.AndAlso(combined, cond);
            }
        }

        if (group.Groups != null)
        {
            foreach (var child in group.Groups)
            {
                var cond = BuildGroupCondition<T>(parameter, child, depth + 1);
                if (cond == null) continue;

                combined = combined == null
                    ? cond
                    : isOr ? Expression.OrElse(combined, cond) : Expression.AndAlso(combined, cond);
            }
        }

        if (combined == null) return null;

        if (group.Negate == true)
            combined = Expression.Not(combined);

        return combined;
    }

    private static Expression? BuildAnyCondition(Expression enumerableMember, string[] innerPathSegments, FilterColumnQuery filter)
    {
        if (innerPathSegments.Length == 0) return null;

        var op = filter.OperatorValue;
        var isEmptyOp =
            string.Equals(op, "isempty", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, "isnotempty", StringComparison.OrdinalIgnoreCase);

        if (!isEmptyOp && string.IsNullOrEmpty(filter.Value)) return null;

        var elementType = GetEnumerableElementType(enumerableMember.Type);
        if (elementType == null) return null;

        var m = Expression.Parameter(elementType, "m");
        Expression? member = m;
        foreach (var seg in innerPathSegments)
        {
            member = TryMemberAccess(member, seg);
            if (member == null) return null;
        }

        var memberCondition = BuildMemberCondition(member, filter);
        if (memberCondition == null) return null;

        // rebind parameter from 'parameter' to 'm'
        var lambda = Expression.Lambda(memberCondition, m);

        var anyCall = Expression.Call(typeof(Enumerable), "Any", new[] { elementType }, enumerableMember, lambda);
        return string.Equals(filter.OperatorValue, "not", StringComparison.OrdinalIgnoreCase) ? Expression.Not(anyCall) : anyCall;
    }

    private static Expression? BuildCountCondition(Expression enumerableMember, FilterColumnQuery filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Value)) return null;
        if (!int.TryParse(filter.Value, out var parsed)) return null;

        var elementType = GetEnumerableElementType(enumerableMember.Type);
        if (elementType == null) return null;

        var countCall = Expression.Call(typeof(Enumerable), "Count", new[] { elementType }, enumerableMember);
        var constant = Expression.Constant(parsed);
        var body = countCall.GetCondition(constant, filter.OperatorValue);
        return string.Equals(filter.OperatorValue, "not", StringComparison.OrdinalIgnoreCase) ? Expression.Not(body) : body;
    }

    private static Expression? BuildMemberCondition(Expression member, FilterColumnQuery filter)
    {
        var op = filter.OperatorValue;
        var isEmptyOp =
            string.Equals(op, "isempty", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, "isnotempty", StringComparison.OrdinalIgnoreCase);

        if (!isEmptyOp && filter.Value == null) return null;

        if ((string.Equals(op, "contains", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "startswith", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "endswith", StringComparison.OrdinalIgnoreCase))
            && member.Type != typeof(string))
            return null;

        var constExpr = filter.Value == null
            ? Expression.Constant(null, member.Type)
            : (member as MemberExpression)?.GetConstant(filter.Value) ?? Expression.Constant(null, member.Type);

        return member.GetCondition(constExpr, filter.OperatorValue);
    }

    private static Expression<Func<T, bool>> FilterInPath<T>(this Expression<Func<T, bool>> predicate, FilterColumnQuery query)
    {
        if (query == null) return predicate;
        if (string.IsNullOrWhiteSpace(query.ColumnField)) return predicate;

        var segments = query.ColumnField.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2) return predicate;

        var rootParam = predicate.Parameters[0];

        // Decide whether first segment is a collection navigation (IEnumerable<>) or a reference navigation.
        var firstMember = TryMemberAccess(rootParam, segments[0]);
        if (firstMember == null) return predicate;

        // collection: Offers.Price => Any(o => o.Price ...)
        if (IsEnumerableButNotString(firstMember.Type))
        {
            // Special case: Offers.Count
            if (segments.Length == 2 && string.Equals(segments[1], "Count", StringComparison.OrdinalIgnoreCase))
                return predicate.FilterInListByCount(query);

            return predicate.FilterInEnumerable(firstMember, segments.Skip(1).ToArray(), query);
        }

        // reference: Customer.Name => Customer.Name ...
        var member = firstMember;
        foreach (var seg in segments.Skip(1))
        {
            member = TryMemberAccess(member, seg);
            if (member == null) return predicate;
        }

        return predicate.FilterInResolvedMember(member, query);
    }

    private static Expression<Func<T, bool>> FilterInList<T>(this Expression<Func<T, bool>> predicate, FilterColumnQuery query)
    {
        if (query == null) return predicate;
        if (string.IsNullOrWhiteSpace(query.ColumnField)) return predicate;

        var fields = query.ColumnField.Split('.');
        if (fields.Length < 2) return predicate;

        var op = query.OperatorValue;
        var isEmptyOp =
            string.Equals(op, "isempty", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, "isnotempty", StringComparison.OrdinalIgnoreCase);

        if (!isEmptyOp && string.IsNullOrEmpty(query.Value)) return predicate;

        var model = fields[0];
        var property = fields[1];

        if (property == "Count") return predicate.FilterInListByCount(query);

        var item = predicate.Parameters[0];

        var models = item.TryPropertyOrField(model);

        if (models == null) return predicate;

        if (models.Type.GenericTypeArguments.Length == 0) return predicate.FilterInModel(query);

        var modelType = models.Type.GenericTypeArguments.FirstOrDefault();

        if (modelType == null) return predicate;

        var m = Expression.Parameter(modelType, "m");

        var propertyExp = m.TryPropertyOrField(property);

        if (propertyExp == null) return predicate;

        if ((string.Equals(op, "contains", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "startswith", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "endswith", StringComparison.OrdinalIgnoreCase))
            && propertyExp.Type != typeof(string))
            return predicate;

        Expression constant = string.IsNullOrEmpty(query.Value)
            ? Expression.Constant(null, propertyExp.Type)
            : propertyExp.GetConstant(query.Value);

        var condition = propertyExp.GetCondition(constant, query.OperatorValue);

        var lambdaExp = Expression.Lambda(condition, m);

        var anyCall = Expression.Call(typeof(Enumerable), "Any", new[] { modelType }, models, lambdaExp);

        Expression listCondition = anyCall;
        if (string.Equals(query.OperatorValue, "not", StringComparison.OrdinalIgnoreCase))
            listCondition = Expression.Not(anyCall);

        var body = Expression.AndAlso(predicate.Body, listCondition);
        return Expression.Lambda<Func<T, bool>>(body, item);
    }

    public static Expression<Func<T, bool>> FilterInProperty<T>(this Expression<Func<T, bool>> predicate, FilterColumnQuery query)
    {
        if (query == null) return predicate;
        if (string.IsNullOrWhiteSpace(query.ColumnField)) return predicate;

        var parameterExp = predicate.Parameters[0];

        // allow case-insensitive property/field
        var pi = parameterExp.Type.GetProperty(query.ColumnField, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        var fi = parameterExp.Type.GetField(query.ColumnField, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (pi == null && fi == null) return predicate;

        var memberName = pi?.Name ?? fi!.Name;
        var propertyExp = Expression.PropertyOrField(parameterExp, memberName);

        var op = query.OperatorValue;
        var isEmptyOp =
            string.Equals(op, "isempty", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, "isnotempty", StringComparison.OrdinalIgnoreCase);

        if (!isEmptyOp && query.Value == null) return predicate;

        if ((string.Equals(op, "contains", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "startswith", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "endswith", StringComparison.OrdinalIgnoreCase))
            && propertyExp.Type != typeof(string))
            return predicate;

        Expression constant = query.Value == null
            ? Expression.Constant(null, propertyExp.Type)
            : propertyExp.GetConstant(query.Value);

        var condition = propertyExp.GetCondition(constant, query.OperatorValue);

        var body = Expression.AndAlso(predicate.Body, condition);
        return Expression.Lambda<Func<T, bool>>(body, parameterExp);
    }

    private static Expression<Func<T, bool>> FilterInListByCount<T>(this Expression<Func<T, bool>> predicate, FilterColumnQuery query)
    {
        if (query == null) return predicate;
        if (string.IsNullOrWhiteSpace(query.ColumnField)) return predicate;

        var fields = query.ColumnField.Split('.');
        if (fields.Length < 2) return predicate;
        if (string.IsNullOrWhiteSpace(query.Value)) return predicate;

        var model = fields[0];

        var item = predicate.Parameters[0];

        var models = item.TryPropertyOrField(model);

        if (models == null) return predicate;

        var modelType = GetEnumerableElementType(models.Type);
        if (modelType == null) return predicate;

        var countCall = Expression.Call(typeof(Enumerable), "Count", new[] { modelType }, models);

        if (!int.TryParse(query.Value, out var parsed))
            return predicate;

        var constant = Expression.Constant(parsed);

        var body = countCall.GetCondition(constant, query.OperatorValue);

        var countCondition = body;
        if (string.Equals(query.OperatorValue, "not", StringComparison.OrdinalIgnoreCase))
            countCondition = Expression.Not(body);

        var finalBody = Expression.AndAlso(predicate.Body, countCondition);
        return Expression.Lambda<Func<T, bool>>(finalBody, item);
    }

    private static Expression<Func<T, bool>> FilterInModel<T>(this Expression<Func<T, bool>> predicate, FilterColumnQuery query)
    {

        if (query == null) return predicate;
        if (string.IsNullOrWhiteSpace(query.ColumnField)) return predicate;

        var fields = query.ColumnField.Split('.');
        if (fields.Length < 2) return predicate;

        var op = query.OperatorValue;
        var isEmptyOp =
            string.Equals(op, "isempty", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, "isnotempty", StringComparison.OrdinalIgnoreCase);

        if (!isEmptyOp && string.IsNullOrEmpty(query.Value)) return predicate;

        var model = fields[0];
        var property = fields[1];

        var item = predicate.Parameters[0];

        var models = item.TryPropertyOrField(model);

        if (models == null) return predicate;

        // property/field access on the nested model (case-sensitive; keep as-is to avoid surprises)
        var propertyExp = Expression.PropertyOrField(models, property);

        if ((string.Equals(op, "contains", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "startswith", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "endswith", StringComparison.OrdinalIgnoreCase))
            && propertyExp.Type != typeof(string))
            return predicate;

        Expression constant = string.IsNullOrEmpty(query.Value)
            ? Expression.Constant(null, propertyExp.Type)
            : propertyExp.GetConstant(query.Value);

        var condition = propertyExp.GetCondition(constant, query.OperatorValue);

        var body = Expression.AndAlso(predicate.Body, condition);
        return Expression.Lambda<Func<T, bool>>(body, item);
    }

    private static Expression<Func<T, bool>> FilterInEnumerable<T>(
        this Expression<Func<T, bool>> predicate,
        Expression enumerableMember,
        string[] innerPathSegments,
        FilterColumnQuery query)
    {
        if (innerPathSegments.Length == 0) return predicate;

        var op = query.OperatorValue;
        var isEmptyOp =
            string.Equals(op, "isempty", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, "isnotempty", StringComparison.OrdinalIgnoreCase);

        if (!isEmptyOp && string.IsNullOrEmpty(query.Value)) return predicate;

        var elementType = GetEnumerableElementType(enumerableMember.Type);
        if (elementType == null) return predicate;

        var m = Expression.Parameter(elementType, "m");
        Expression? member = m;
        foreach (var seg in innerPathSegments)
        {
            member = TryMemberAccess(member, seg);
            if (member == null) return predicate;
        }

        if ((string.Equals(op, "contains", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "startswith", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "endswith", StringComparison.OrdinalIgnoreCase))
            && member.Type != typeof(string))
            return predicate;

        Expression constant = string.IsNullOrEmpty(query.Value)
            ? Expression.Constant(null, member.Type)
            : ((member as MemberExpression)!).GetConstant(query.Value);

        var condition = member.GetCondition(constant, query.OperatorValue);
        var lambdaExp = Expression.Lambda(condition, m);

        var anyCall = Expression.Call(typeof(Enumerable), "Any", new[] { elementType }, enumerableMember, lambdaExp);

        Expression listCondition = anyCall;
        if (string.Equals(query.OperatorValue, "not", StringComparison.OrdinalIgnoreCase))
            listCondition = Expression.Not(anyCall);

        var body = Expression.AndAlso(predicate.Body, listCondition);
        return Expression.Lambda<Func<T, bool>>(body, predicate.Parameters[0]);
    }

    private static Expression<Func<T, bool>> FilterInResolvedMember<T>(
        this Expression<Func<T, bool>> predicate,
        Expression member,
        FilterColumnQuery query)
    {
        var op = query.OperatorValue;
        var isEmptyOp =
            string.Equals(op, "isempty", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, "isnotempty", StringComparison.OrdinalIgnoreCase);

        if (!isEmptyOp && query.Value == null) return predicate;

        if ((string.Equals(op, "contains", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "startswith", StringComparison.OrdinalIgnoreCase)
            || string.Equals(op, "endswith", StringComparison.OrdinalIgnoreCase))
            && member.Type != typeof(string))
            return predicate;

        Expression constant = query.Value == null
            ? Expression.Constant(null, member.Type)
            : ((member as MemberExpression)!).GetConstant(query.Value);

        var condition = member.GetCondition(constant, query.OperatorValue);
        var body = Expression.AndAlso(predicate.Body, condition);
        return Expression.Lambda<Func<T, bool>>(body, predicate.Parameters[0]);
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
}
