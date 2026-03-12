using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
public static class EfKeysetPagingExtensions
{
    public static IQueryable<T> ApplyKeysetPaging<T, TSort, TRow>(this IQueryable<T> query, ListRequest req, Expression<Func<T, TSort>> sortKey, Expression<Func<T, TRow>> rowKey)
        where TSort : IComparable<TSort>
        where TRow : IComparable<TRow>
    {
        var param = sortKey.Parameters[0];

        var sortBody = NormalizeSortExpression(RebindParameter(sortKey.Body, sortKey.Parameters[0], param));
        var rowBody = RebindParameter(rowKey.Body, rowKey.Parameters[0], param);

        if (req?.HasCursor == true)
        {
            var lastSort = Parse<TSort>(req.NextPartitionKey);
            var lastRow = Parse<TRow>(req.NextRowKey);

            var lastSortConst = Expression.Constant(NormalizeSortValue(lastSort), sortBody.Type);
            var lastRowConst = Expression.Constant(lastRow, typeof(TRow));

            var sortLess = BuildLessThan(sortBody, lastSortConst, sortBody.Type);
            var sortEq = BuildEqual(sortBody, lastSortConst, sortBody.Type);
            var rowLess = BuildLessThan(rowBody, lastRowConst, typeof(TRow));

            var predicateBody = Expression.OrElse(sortLess, Expression.AndAlso(sortEq, rowLess));
            var predicate = Expression.Lambda<Func<T, bool>>(predicateBody, param);

            query = query.Where(predicate);
        }

        var ordered = ApplyOrderByDescending(query, sortBody, param);
        return ordered.ThenByDescending(rowKey).Take(req.PageSize + 1);
    }

    private static IOrderedQueryable<T> ApplyOrderByDescending<T>(IQueryable<T> query, Expression sortBody, ParameterExpression param)
    {
        var lambdaType = typeof(Func<,>).MakeGenericType(typeof(T), sortBody.Type);
        var lambda = Expression.Lambda(lambdaType, sortBody, param);

        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == nameof(Queryable.OrderByDescending)
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2);

        var generic = method.MakeGenericMethod(typeof(T), sortBody.Type);
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

    private static Expression BuildLessThan(Expression left, Expression right, Type type)
    {
        var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;

        if (nonNullableType == typeof(string))
            return BuildCompareToLessThan(left, right, typeof(string));

        if (nonNullableType == typeof(Guid))
            return BuildCompareToLessThan(left, right, typeof(Guid));

        if (nonNullableType == typeof(DateOnly))
        {
            if (Nullable.GetUnderlyingType(type) == null)
                return Expression.LessThan(left, right);

            var leftHasValue = Expression.Property(left, nameof(Nullable<DateOnly>.HasValue));
            var rightHasValue = Expression.Property(right, nameof(Nullable<DateOnly>.HasValue));
            var leftValue = Expression.Property(left, nameof(Nullable<DateOnly>.Value));
            var rightValue = Expression.Property(right, nameof(Nullable<DateOnly>.Value));

            return Expression.AndAlso(Expression.AndAlso(leftHasValue, rightHasValue), Expression.LessThan(leftValue, rightValue));
        }

        return Expression.LessThan(left, right);
    }

    private static Expression BuildEqual(Expression left, Expression right, Type type)
    {
        var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;

        if (nonNullableType == typeof(string))
            return BuildCompareToEqual(left, right, typeof(string));

        if (nonNullableType == typeof(Guid))
            return BuildCompareToEqual(left, right, typeof(Guid));

        return Expression.Equal(left, right);
    }

    private static Expression BuildCompareToLessThan(Expression left, Expression right, Type type)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(left.Type);

        if (nullableUnderlying != null)
        {
            var leftHasValue = Expression.Property(left, "HasValue");
            var rightHasValue = Expression.Property(right, "HasValue");
            var leftValue = Expression.Property(left, "Value");
            var rightValue = Expression.Property(right, "Value");

            var compare = BuildNonNullableCompareToLessThan(leftValue, rightValue, nullableUnderlying);
            return Expression.AndAlso(Expression.AndAlso(leftHasValue, rightHasValue), compare);
        }

        return BuildNonNullableCompareToLessThan(left, right, type);
    }

    private static Expression BuildCompareToEqual(Expression left, Expression right, Type type)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(left.Type);

        if (nullableUnderlying != null)
        {
            var leftHasValue = Expression.Property(left, "HasValue");
            var rightHasValue = Expression.Property(right, "HasValue");
            var leftValue = Expression.Property(left, "Value");
            var rightValue = Expression.Property(right, "Value");

            var compare = BuildNonNullableCompareToEqual(leftValue, rightValue, nullableUnderlying);
            return Expression.AndAlso(Expression.AndAlso(leftHasValue, rightHasValue), compare);
        }

        return BuildNonNullableCompareToEqual(left, right, type);
    }

    private static Expression BuildNonNullableCompareToLessThan(Expression left, Expression right, Type type)
    {
        var compareTo = type.GetMethod(nameof(IComparable.CompareTo), new[] { type });
        if (compareTo == null)
            throw new InvalidOperationException($"Could not find CompareTo({type.Name}) on {type.Name}.");

        var compareCall = Expression.Call(left, compareTo, right);
        return Expression.LessThan(compareCall, Expression.Constant(0));
    }

    private static Expression BuildNonNullableCompareToEqual(Expression left, Expression right, Type type)
    {
        var compareTo = type.GetMethod(nameof(IComparable.CompareTo), new[] { type });
        if (compareTo == null)
            throw new InvalidOperationException($"Could not find CompareTo({type.Name}) on {type.Name}.");

        var compareCall = Expression.Call(left, compareTo, right);
        return Expression.Equal(compareCall, Expression.Constant(0));
    }

    private static Expression RebindParameter(Expression body, ParameterExpression from, ParameterExpression to)
    {
        return new ParameterReplaceVisitor(from, to).Visit(body);
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

        object parsed;

        if (nonNullableType == typeof(string))
        {
            parsed = value;
        }
        else if (nonNullableType == typeof(Guid))
        {
            parsed = Guid.Parse(value);
        }
        else if (nonNullableType == typeof(DateTime))
        {
            parsed = DateTime.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
        else if (nonNullableType == typeof(DateOnly))
        {
            parsed = DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        else if (nonNullableType.IsEnum)
        {
            parsed = Enum.Parse(nonNullableType, value, ignoreCase: true);
        }
        else
        {
            parsed = Convert.ChangeType(value, nonNullableType, CultureInfo.InvariantCulture);
        }

        return (T)parsed;
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

public interface ICacheScope
{
    string Key { get; }
    TimeSpan Ttl { get; }
}

public sealed record CacheScope(string Key, TimeSpan Ttl) : ICacheScope;

public sealed record CacheSpec(string Scope, TimeSpan Ttl);

public static class EfCacheScopeExtensions
{
    public static IQueryable<T> CacheScope<T>(this IQueryable<T> query, ICacheScope scope)
        => query.Provider.CreateQuery<T>(
            Expression.Call(
                instance: null,
                method: CacheScopeMethod.MakeGenericMethod(typeof(T)),
                arguments: new Expression[]
                {
                query.Expression,
                Expression.Constant(scope.Key),
                Expression.Constant(scope.Ttl)
                }));


    private static readonly MethodInfo CacheScopeMethod =
        typeof(EfCacheScopeExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(CacheScope) && m.IsGenericMethodDefinition);

    internal static bool TryStrip<T>(this IQueryable<T> query, out IQueryable<T> stripped, out CacheSpec spec)
    {
        if (query.Expression is MethodCallExpression mc &&
            mc.Method.IsGenericMethod &&
            mc.Method.GetGenericMethodDefinition() == CacheScopeMethod)
        {
            var scope = (string)((ConstantExpression)mc.Arguments[1]).Value;
            var ttl = (TimeSpan)((ConstantExpression)mc.Arguments[2]).Value;
            spec = new CacheSpec(scope, ttl);

            stripped = query.Provider.CreateQuery<T>(mc.Arguments[0]);
            return true;
        }

        stripped = query;
        spec = null;
        return false;
    }
}

public static partial class EfListResponseExtensions
{
    private static readonly AsyncLocal<ICacheProvider> _current = new();

    public static ICacheProvider Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    private const string DateOnlyCursorFormat = "yyyy-MM-dd";

    public static async Task<ListResponse<TOut>> ToListResponseAsync<TIn, TOut, TPartition, TRow>(this IQueryable<TIn> query, Func<TIn, Task<TOut>> mapAsync, Func<TOut, TPartition> partitionKeySelector, Func<TOut, TRow> rowKeySelector, bool parallel = true, CancellationToken ct = default) where TOut : class
    {
        return await query.ToListResponseAsync(new ListRequest { PageSize = int.MaxValue }, mapAsync, partitionKeySelector, rowKeySelector, parallel, ct).ConfigureAwait(false);
    }

    public static async Task<ListResponse<TOut>> ToListResponseAsync<TIn, TOut, TPartition, TRow>(this IQueryable<TIn> query, ListRequest request, Func<TIn, Task<TOut>> mapAsync, Func<TOut, TPartition> partitionKeySelector, Func<TOut, TRow> rowKeySelector, bool parallel = true, CancellationToken ct = default) where TOut : class
    {
        return await ToListResponseCoreAsync(query, request, mapAsync, partitionKeySelector, rowKeySelector, parallel, ct).ConfigureAwait(false);
    }

    public static async Task<ListResponse<TOut>> ToListResponseAsync<TIn, TOut, TPartition, TRow>(this IQueryable<TIn> query, ICacheProvider cache, ListRequest request, Func<TIn, Task<TOut>> mapAsync, Func<TOut, TPartition> partitionKeySelector, Func<TOut, TRow> rowKeySelector, bool parallel = true, CancellationToken ct = default) where TOut : class
    {
        return await ToListResponseWithCacheCoreAsync(query, cache, request, mapAsync, partitionKeySelector, rowKeySelector, parallel, ct).ConfigureAwait(false);
    }

    private static async Task<ListResponse<TOut>> ToListResponseCoreAsync<TIn, TOut, TPartition, TRow>(IQueryable<TIn> query, ListRequest request, Func<TIn, Task<TOut>> mapAsync, Func<TOut, TPartition> partitionKeySelector, Func<TOut, TRow> rowKeySelector, bool parallel, CancellationToken ct) where TOut : class
    {
        var dtos = await query.ToListAsync(ct).ConfigureAwait(false);

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

        var hasMoreRecords = mapped.Count > request.PageSize;
        var model = hasMoreRecords ? mapped.Take(request.PageSize).ToList() : mapped.ToList();

        string nextPartitionKey = null;
        string nextRowKey = null;

        if (model.Count > 0)
        {
            var lastReturned = model[model.Count - 1];
            nextPartitionKey = SerializeCursorValue(partitionKeySelector(lastReturned));
            nextRowKey = SerializeCursorValue(rowKeySelector(lastReturned));
        }

        return ListResponse<TOut>.Create(model, request, hasMoreRecords, nextPartitionKey, nextRowKey);
    }

    private static async Task<ListResponse<TOut>> ToListResponseWithCacheCoreAsync<TIn, TOut, TPartition, TRow>(IQueryable<TIn> query, ICacheProvider cache, ListRequest request, Func<TIn, Task<TOut>> mapAsync, Func<TOut, TPartition> partitionKeySelector, Func<TOut, TRow> rowKeySelector, bool parallel, CancellationToken ct) where TOut : class
    {
        query.TryStrip(out var stripped, out var cacheSpec);

        if (cacheSpec == null || cache == null)
            return await ToListResponseCoreAsync(stripped, request, mapAsync, partitionKeySelector, rowKeySelector, parallel, ct).ConfigureAwait(false);

        var verKey = $"{cacheSpec.Scope}:ver";
        var ver = await cache.GetLongAsync(verKey).ConfigureAwait(false);

        var reqSig = ListRequestCacheSig.ReqSig(request);
        var dataKey = $"{cacheSpec.Scope}:v{ver}:{reqSig}";

        var cached = await cache.GetAsync<ListResponse<TOut>>(dataKey).ConfigureAwait(false);
        if (cached != null)
            return cached;

        var result = await ToListResponseCoreAsync(stripped, request, mapAsync, partitionKeySelector, rowKeySelector, parallel, ct).ConfigureAwait(false);

        await cache.AddAsync(dataKey, result, cacheSpec.Ttl).ConfigureAwait(false);
        return result;
    }

    private static string SerializeCursorValue<T>(T value)
    {
        if (value == null)
            return null;

        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (type == typeof(string))
            return (string)(object)value;

        if (type == typeof(DateOnly))
            return ((DateOnly)(object)value).ToString(DateOnlyCursorFormat, CultureInfo.InvariantCulture);

        if (type == typeof(DateTime))
            return ((DateTime)(object)value).ToString("O", CultureInfo.InvariantCulture);

        if (type == typeof(Guid))
            return ((Guid)(object)value).ToString();

        if (type.IsEnum)
            return value.ToString();

        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return value.ToString();
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

    public static async Task<TOut> SingleOrDefaultMapAsync<TIn, TOut>(
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