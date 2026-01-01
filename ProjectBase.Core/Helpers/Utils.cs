using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace ProjectBase.Core.Helpers;

public static class Utils
{
    public static Expression GetConstant(this MemberExpression member, string value)
    {
        var constantValue = member.ConvertConstant(value);
        var targetType = member.Type;
        return Expression.Constant(constantValue, targetType);
    }

    public static Expression GetCondition(this Expression member, Expression constant, string? OperatorValue)
    {
        var op = OperatorValue ?? "equals";

        return op.GetFilterOperator() switch
        {
            FilterOperator.GreaterThan => Compare(member, constant, ExpressionType.GreaterThan),
            FilterOperator.GreaterThanOrEqual => Compare(member, constant, ExpressionType.GreaterThanOrEqual),
            FilterOperator.LessThan => Compare(member, constant, ExpressionType.LessThan),
            FilterOperator.LessThanOrEqual => Compare(member, constant, ExpressionType.LessThanOrEqual),
            FilterOperator.Equals => Expression.Equal(member, AlignConstant(constant, member.Type)),
            FilterOperator.Contains => StringCall(member, constant, nameof(string.Contains)),
            FilterOperator.EndsWith => StringCall(member, constant, nameof(string.EndsWith)),
            FilterOperator.StartsWith => StringCall(member, constant, nameof(string.StartsWith)),
            FilterOperator.IsEmpty => EmptyCondition(member, isNotEmpty: false),
            FilterOperator.IsNotEmpty => EmptyCondition(member, isNotEmpty: true),
            FilterOperator.Is => Expression.Equal(member, AlignConstant(constant, member.Type)),
            FilterOperator.Not => Expression.NotEqual(member, AlignConstant(constant, member.Type)),
            FilterOperator.None => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };

    }

    private enum FilterOperator
    {
        None,
        IsEmpty,
        IsNotEmpty,
        Is,
        Not,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        Equals,
        StartsWith,
        EndsWith
    }

    private static FilterOperator GetFilterOperator(this string? OperatorValue) => (OperatorValue ?? "").ToLowerInvariant() switch
    {
        "isempty" => FilterOperator.IsEmpty,
        "isnotempty" => FilterOperator.IsNotEmpty,
        "is" => FilterOperator.Is,
        "not" => FilterOperator.Not,
        ">" => FilterOperator.GreaterThan,
        ">=" => FilterOperator.GreaterThanOrEqual,
        "<" => FilterOperator.LessThan,
        "<=" => FilterOperator.LessThanOrEqual,

        "=" => FilterOperator.Equals,
        "equals" => FilterOperator.Equals,

        "contains" => FilterOperator.Contains,
        "startswith" => FilterOperator.StartsWith,
        "endswith" => FilterOperator.EndsWith,

        _ => FilterOperator.None
    };

    private static object? ConvertConstant(this MemberExpression member, string value)
    {
        var targetType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, ignoreCase: true);

        if (targetType == typeof(Guid))
            return Guid.Parse(value);

        if (targetType == typeof(DateTime))
            return DateTime.Parse(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(bool))
            return bool.Parse(value);

        if (targetType == typeof(string))
            return value;

        // numeric, etc.
        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static Expression AlignConstant(Expression constant, Type targetType)
    {
        if (constant.Type == targetType) return constant;
        return Expression.Convert(constant, targetType);
    }

    private static Expression Compare(Expression member, Expression constant, ExpressionType comparisonType)
    {
        var memberType = member.Type;
        var underlying = Nullable.GetUnderlyingType(memberType);
        if (underlying is null)
        {
            var aligned = AlignConstant(constant, memberType);
            return Expression.MakeBinary(comparisonType, member, aligned);
        }

        // nullable comparisons: member.HasValue && member.Value OP constant
        var hasValue = Expression.Property(member, "HasValue");
        var valueExpr = Expression.Property(member, "Value"); // underlying type
        var alignedConstant = AlignConstant(constant, underlying);
        var comparison = Expression.MakeBinary(comparisonType, valueExpr, alignedConstant);
        return Expression.AndAlso(hasValue, comparison);
    }


    public static string GetSortMethod(this string? OperatorValue) => (OperatorValue ?? "").ToLowerInvariant() switch
    {
        "asc" => nameof(Queryable.OrderBy),
        "desc" => nameof(Queryable.OrderByDescending),
        _ => ""
    };

    public static MemberExpression? TryPropertyOrField(this ParameterExpression parameter, string propertyName)
    {
        try
        {
            return Expression.PropertyOrField(parameter, propertyName);
        }
        catch
        {
            return null;
        }

    }

    private static Expression StringCall(Expression member, Expression constant, string methodName)
    {
        if (member.Type != typeof(string))
            throw new NotSupportedException($"'{methodName}' operator is only supported for string members.");

        var method = typeof(string)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m =>
                string.Equals(m.Name, methodName, StringComparison.Ordinal)
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(string));

        if (method is null)
            throw new MissingMethodException(typeof(string).FullName, methodName);

        var aligned = AlignConstant(constant, typeof(string));
        return Expression.Call(member, method, aligned);
    }

    private static Expression EmptyCondition(Expression member, bool isNotEmpty)
    {
        // string: null/empty
        if (member.Type == typeof(string))
        {
            var nullConst = Expression.Constant(null, typeof(string));
            var emptyConst = Expression.Constant("", typeof(string));
            var isNull = Expression.Equal(member, nullConst);
            var isEmpty = Expression.Equal(member, emptyConst);
            var isNullOrEmpty = Expression.OrElse(isNull, isEmpty);
            return isNotEmpty ? Expression.Not(isNullOrEmpty) : isNullOrEmpty;
        }

        // nullable: null / not null
        if (Nullable.GetUnderlyingType(member.Type) is not null)
        {
            var nullConst = Expression.Constant(null, member.Type);
            var isNull = Expression.Equal(member, nullConst);
            return isNotEmpty ? Expression.Not(isNull) : isNull;
        }

        // value types: default(T) / not default(T)
        var def = Expression.Default(member.Type);
        var eq = Expression.Equal(member, def);
        return isNotEmpty ? Expression.Not(eq) : eq;
    }
}