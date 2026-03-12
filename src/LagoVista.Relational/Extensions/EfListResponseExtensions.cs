using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
