using LagoVista.Core.Models.UIMetaData;
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

public static class EfKeysetPagingExtensions
{
    public enum KeysetPagingDirection
    {
        Ascending,
        Descending
    }

    public static IQueryable<T> ApplyKeysetPaging<T, TSort, TRow>(this IQueryable<T> query, ListRequest req, Expression<Func<T, TSort>> sortKey, Expression<Func<T, TRow>> rowKey, KeysetPagingDirection direction = KeysetPagingDirection.Ascending)
        where TSort : IComparable<TSort>
        where TRow : IComparable<TRow>
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        if (sortKey == null) throw new ArgumentNullException(nameof(sortKey));
        if (rowKey == null) throw new ArgumentNullException(nameof(rowKey));
        if (req.PageSize <= 0) throw new ArgumentOutOfRangeException(nameof(req.PageSize));

        var param = sortKey.Parameters[0];
        var sortBody = NormalizeSortExpression(sortKey.Body);
        var rowBody = RebindParameter(rowKey.Body, rowKey.Parameters[0], param);

        if (req.HasCursor)
        {
            var lastSort = Parse<TSort>(req.NextPartitionKey);
            var lastRow = Parse<TRow>(req.NextRowKey);

            var lastSortConst = Expression.Constant(NormalizeSortValue(lastSort), sortBody.Type);
            var lastRowConst = Expression.Constant(lastRow, typeof(TRow));

            var sortCompare = BuildDirectionalComparison(sortBody, lastSortConst, sortBody.Type, direction);
            var sortEq = BuildComparison(sortBody, lastSortConst, sortBody.Type, ComparisonOperator.Equal);
            var rowCompare = BuildDirectionalComparison(rowBody, lastRowConst, typeof(TRow), direction);

            var predicateBody = Expression.OrElse(sortCompare, Expression.AndAlso(sortEq, rowCompare));
            var predicate = Expression.Lambda<Func<T, bool>>(predicateBody, param);
            query = query.Where(predicate);
        }

        var ordered = ApplyOrder(query, sortBody, param, direction, thenBy: false);
        ordered = ApplyOrder(ordered, rowBody, param, direction, thenBy: true);

        return ordered.Take(req.PageSize + 1);
    }

    private enum ComparisonOperator
    {
        LessThan,
        GreaterThan,
        Equal
    }

    private static Expression BuildDirectionalComparison(Expression left, Expression right, Type type, KeysetPagingDirection direction)
    {
        return BuildComparison(left, right, type, direction == KeysetPagingDirection.Descending ? ComparisonOperator.LessThan : ComparisonOperator.GreaterThan);
    }

    private static Expression BuildComparison(Expression left, Expression right, Type type, ComparisonOperator op)
    {
        var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;

        if (Nullable.GetUnderlyingType(left.Type) != null)
        {
            var leftHasValue = Expression.Property(left, nameof(Nullable<int>.HasValue));
            var rightHasValue = Expression.Property(right, nameof(Nullable<int>.HasValue));
            var leftValue = Expression.Property(left, nameof(Nullable<int>.Value));
            var rightValue = Expression.Property(right, nameof(Nullable<int>.Value));

            var inner = BuildNonNullableComparison(leftValue, rightValue, nonNullableType, op);
            return Expression.AndAlso(Expression.AndAlso(leftHasValue, rightHasValue), inner);
        }

        return BuildNonNullableComparison(left, right, nonNullableType, op);
    }

    private static Expression BuildNonNullableComparison(Expression left, Expression right, Type type, ComparisonOperator op)
    {
        if (type == typeof(string) || type == typeof(Guid))
        {
            var compareTo = type.GetMethod(nameof(IComparable.CompareTo), new[] { type });
            if (compareTo == null) throw new InvalidOperationException($"Could not find CompareTo({type.Name}) on {type.Name}.");

            var compareCall = Expression.Call(left, compareTo, right);

            return op switch
            {
                ComparisonOperator.LessThan => Expression.LessThan(compareCall, Expression.Constant(0)),
                ComparisonOperator.GreaterThan => Expression.GreaterThan(compareCall, Expression.Constant(0)),
                ComparisonOperator.Equal => Expression.Equal(compareCall, Expression.Constant(0)),
                _ => throw new NotSupportedException($"Unsupported operator: {op}")
            };
        }

        return op switch
        {
            ComparisonOperator.LessThan => Expression.LessThan(left, right),
            ComparisonOperator.GreaterThan => Expression.GreaterThan(left, right),
            ComparisonOperator.Equal => Expression.Equal(left, right),
            _ => throw new NotSupportedException($"Unsupported operator: {op}")
        };
    }

    private static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> query, Expression keyBody, ParameterExpression param, KeysetPagingDirection direction, bool thenBy)
    {
        var lambdaType = typeof(Func<,>).MakeGenericType(typeof(T), keyBody.Type);
        var lambda = Expression.Lambda(lambdaType, keyBody, param);

        string methodName;

        if (thenBy)
        {
            methodName = direction == KeysetPagingDirection.Descending
                ? nameof(Queryable.ThenByDescending)
                : nameof(Queryable.ThenBy);
        }
        else
        {
            methodName = direction == KeysetPagingDirection.Descending
                ? nameof(Queryable.OrderByDescending)
                : nameof(Queryable.OrderBy);
        }

        var method = typeof(Queryable).GetMethods().Single(m => m.Name == methodName && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
        var generic = method.MakeGenericMethod(typeof(T), keyBody.Type);

        return (IOrderedQueryable<T>)generic.Invoke(null, new object[] { query, lambda });
    }

    private static Expression NormalizeSortExpression(Expression body)
    {
        var nonNullableType = Nullable.GetUnderlyingType(body.Type) ?? body.Type;

        if (nonNullableType == typeof(decimal))
            return Expression.Convert(body, typeof(double));

        return body;
    }

    private static object NormalizeSortValue<T>(T value)
    {
        if (value == null)
            return null;

        var nonNullableType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (nonNullableType == typeof(decimal))
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);

        return value;
    }

    private static Expression RebindParameter(Expression body, ParameterExpression from, ParameterExpression to)
    {
        return from == to ? body : new ParameterReplaceVisitor(from, to).Visit(body);
    }

    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _from ? _to : base.VisitParameter(node);
        }
    }

    private static T Parse<T>(string value)
    {
        var targetType = typeof(T);
        var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (string.IsNullOrWhiteSpace(value))
            return default;

        object parsed = nonNullableType switch
        {
            var t when t == typeof(string) => value,
            var t when t == typeof(Guid) => Guid.Parse(value),
            var t when t == typeof(DateTime) => DateTime.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            var t when t == typeof(DateOnly) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            var t when t.IsEnum => Enum.Parse(t, value, ignoreCase: true),
            _ => Convert.ChangeType(value, nonNullableType, CultureInfo.InvariantCulture)
        };

        return (T)parsed;
    }
}