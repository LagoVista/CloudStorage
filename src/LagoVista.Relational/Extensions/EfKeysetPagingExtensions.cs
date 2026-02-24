using LagoVista.Core.Exceptions;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

public static class EfKeysetPagingExtensions
{
    /// <summary>
    /// Applies keyset (seek) paging using ListRequest.NextPartitionKey/NextRowKey as a cursor.
    /// Ordering: sortKey DESC, rowKey DESC.
    /// Cursor means: fetch items strictly "after" the cursor in that ordering.
    /// </summary>
    public static IQueryable<T> ApplyKeysetPaging<T, TSort, TRow>(
        this IQueryable<T> query,
        ListRequest req,
        Expression<Func<T, TSort>> sortKey,
        Expression<Func<T, TRow>> rowKey)
        where TSort : IComparable<TSort>
        where TRow : IComparable<TRow>
    {
        if (req?.HasCursor == true)
        {
            // Parse cursor from strings
            var lastSort = Parse<TSort>(req.NextPartitionKey);
            var lastRow = Parse<TRow>(req.NextRowKey);

            // Build: x => sort(x) < lastSort || (sort(x) == lastSort && row(x) < lastRow)
            var param = sortKey.Parameters[0];

            var sortBody = Expression.Invoke(sortKey, param);
            var rowBody = Expression.Invoke(rowKey, param);

            var lastSortConst = Expression.Constant(lastSort, typeof(TSort));
            var lastRowConst = Expression.Constant(lastRow, typeof(TRow));

            var sortLess = Expression.LessThan(sortBody, lastSortConst);
            var sortEq = Expression.Equal(sortBody, lastSortConst);
            var rowLess = Expression.LessThan(rowBody, lastRowConst);

            var predicateBody = Expression.OrElse(sortLess, Expression.AndAlso(sortEq, rowLess));
            var predicate = Expression.Lambda<Func<T, bool>>(predicateBody, param);

            query = query.Where(predicate);
        }

        var pageSize = req.PageSize;
        var takeSize = pageSize + 1;

        return query
            .OrderByDescending(sortKey)
            .ThenByDescending(rowKey)
            .Take(takeSize);
    }

    private static T Parse<T>(string value)
    {
        if (typeof(T) == typeof(DateTime))
            return (T)(object)DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);

        if (typeof(T) == typeof(DateTimeOffset))
            return (T)(object)DateTimeOffset.Parse(value, null, DateTimeStyles.RoundtripKind);

        if (typeof(T) == typeof(Guid))
            return (T)(object)Guid.Parse(value);

        // Most primitives, enums, strings
        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }
}

public static class ListRequestCursorWriter
{
    public static void SetNextCursor<T>(
        this ListRequest req,
        T lastItem,
        Func<T, DateTime> sortKey,
        Func<T, string> rowKey)
    {
        if (lastItem == null)
        {
            req.NextPartitionKey = null;
            req.NextRowKey = null;
            return;
        }

        req.NextPartitionKey = sortKey(lastItem).ToString("O");
        req.NextRowKey = rowKey(lastItem);
    }
}

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

public static class EfListResponseExtensions
{
    public static async Task<ListResponse<TOut>> ToListResponseAsync<TIn, TOut>(this IQueryable<TIn> query, Func<TIn, Task<TOut>> mapAsync, Func<TOut, DateTime> partitionKeySelector, Func<TOut, string> rowKeySelector, bool parallel = true, CancellationToken ct = default) where TOut : class
    {
        return await query.ToListResponseAsync(new ListRequest { PageSize = int.MaxValue }, mapAsync, partitionKeySelector, rowKeySelector, parallel, ct).ConfigureAwait(false);
    }

    public static async Task<ListResponse<TOut>> ToListResponseAsync<TIn, TOut>(this IQueryable<TIn> query, ListRequest request, Func<TIn, Task<TOut>> mapAsync, Func<TOut, DateTime> partitionKeySelector, Func<TOut, string> rowKeySelector, bool parallel = true, CancellationToken ct = default) where TOut : class
    {
        // Materialize (EF must execute on the server first)
        var dtos = await query.ToListAsync(ct).ConfigureAwait(false);

        // Async map
        IReadOnlyList<TOut> mapped;

        if (parallel)
        {
            mapped = await Task.WhenAll(dtos.Select(mapAsync)).ConfigureAwait(false);
        }
        else
        {
            var list = new List<TOut>(dtos.Count);
            foreach (var dto in dtos)
                list.Add(await mapAsync(dto).ConfigureAwait(false));
            mapped = list;
        }

        return ListResponse<TOut>.Create(mapped, request, partitionKeySelector, rowKeySelector);
    }

    public static async Task<ListResponse<TOut>> ToListResponseAsync<TIn, TOut>(this IQueryable<TIn> query, Func<TIn, Task<TOut>> mapAsync, Func<TOut, string> partitionKeySelector, Func<TOut, string> rowKeySelector, bool parallel = true, CancellationToken ct = default) where TOut : class
    {
        return await query.ToListResponseAsync(new ListRequest { PageSize = int.MaxValue }, mapAsync, partitionKeySelector, rowKeySelector, parallel, ct).ConfigureAwait(false);
    }

    public static async Task<ListResponse<TOut>> ToListResponseAsync<TIn, TOut>(this IQueryable<TIn> query, ListRequest request, Func<TIn, Task<TOut>> mapAsync, Func<TOut, string> partitionKeySelector, Func<TOut, string> rowKeySelector, bool parallel = true, CancellationToken ct = default) where TOut : class
    {
        // Materialize (EF must execute on the server first)
        var dtos = await query.ToListAsync(ct).ConfigureAwait(false);

        // Async map
        IReadOnlyList<TOut> mapped;

        if (parallel)
        {
            mapped = await Task.WhenAll(dtos.Select(mapAsync)).ConfigureAwait(false);
        }
        else
        {
            var list = new List<TOut>(dtos.Count);
            foreach (var dto in dtos)
                list.Add(await mapAsync(dto).ConfigureAwait(false));
            mapped = list;
        }

        return ListResponse<TOut>.Create(mapped, request, partitionKeySelector, rowKeySelector);
    }

}

public static class EfSingleMapExtensions
{
    public static async Task<TOut> SingleMapAsync<TIn, TOut>(
        this IQueryable<TIn> query,
        Func<TIn, Task<TOut>> mapAsync,
        CancellationToken ct = default)
    {
        var dto = await query.SingleAsync(ct).ConfigureAwait(false);
        return await mapAsync(dto).ConfigureAwait(false);
    }

    public static async Task<TOut?> SingleOrDefaultMapAsync<TIn, TOut>(
        this IQueryable<TIn> query,
        Func<TIn, Task<TOut>> mapAsync,
        CancellationToken ct = default)
        where TOut : class
    {
        var dto = await query.SingleOrDefaultAsync(ct).ConfigureAwait(false);
        if (dto == null) return null;
        return await mapAsync(dto).ConfigureAwait(false);
    }

    public static async Task<TOut?> FirstOrDefaultMapAsync<TIn, TOut>(
        this IQueryable<TIn> query,
        Func<TIn, Task<TOut>> mapAsync,
        CancellationToken ct = default)
        where TOut : class
    {
        var dto = await query.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (dto == null) return null;
        return await mapAsync(dto).ConfigureAwait(false);
    }
}

public static class DbSetQueryExtensions
{
    public static IQueryable<T> ReadonlyQuery<T>(this DbSet<T> set)
        where T : class
        => set.AsNoTracking();
}