using ProjectBase.Core.Entities;
using ProjectBase.Core.Pagination;
using System.Linq.Expressions;
using System.Reflection;

namespace ProjectBase.Core.Helpers;

public static class FilterQueryHelper
{
    public static Expression<Func<T, bool>> Filter<T>(this Expression<Func<T, bool>> predicate, PaginationFilterQuery query) where T : BaseEntity
    {
        if (query?.Filters == null) return predicate;

        var columnField = query.Filters.ColumnField;
        if (string.IsNullOrWhiteSpace(columnField)) return predicate;

        if (columnField.IndexOf('.') > -1)
            return predicate.FilterInList(query.Filters);

        return predicate.FilterInProperty(query.Filters);
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

        var modelType = models.Type.GenericTypeArguments.FirstOrDefault();

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
}
