using LagoVista.Core.Exceptions;
using LagoVista.Core.Models.UIMetaData;
using System;
using System.Linq;
using System.Linq.Expressions;

public static class EfListRequestDateFilters
{
    public static IQueryable<T> ApplyDateFilter<T>(this IQueryable<T> query, ListRequest request, Expression<Func<T, DateOnly>> dateSelector)
    {
        
        if (request == null)
            return query;

        if (!request.TryGetDateRange(out var start, out var endExcl, out var err))
        {
            throw new ValidationException(err, true, err);
        }

        // No dates provided => no-op
        if (!start.HasValue && !endExcl.HasValue)
            return query;

        DateOnly? startDay = start.HasValue ? DateOnly.FromDateTime(start.Value) : null;
        DateOnly? endDayExcl = endExcl.HasValue ? DateOnly.FromDateTime(endExcl.Value) : null;

        if (startDay.HasValue)
            query = query.Where(GreaterOrEqual(dateSelector, startDay.Value));

        if (endDayExcl.HasValue)
            query = query.Where(LessThan(dateSelector, endDayExcl.Value));

        return query;
    }

    private static Expression<Func<T, bool>> GreaterOrEqual<T>(
        Expression<Func<T, DateOnly>> selector,
        DateOnly value)
    {
        var p = selector.Parameters[0];
        var body = selector.Body;
        var c = Expression.Constant(value, typeof(DateOnly));
        return Expression.Lambda<Func<T, bool>>(Expression.GreaterThanOrEqual(body, c), p);
    }

    private static Expression<Func<T, bool>> LessThan<T>(
        Expression<Func<T, DateOnly>> selector,
        DateOnly value)
    {
        var p = selector.Parameters[0];
        var body = selector.Body;
        var c = Expression.Constant(value, typeof(DateOnly));
        return Expression.Lambda<Func<T, bool>>(Expression.LessThan(body, c), p);
    }
}
