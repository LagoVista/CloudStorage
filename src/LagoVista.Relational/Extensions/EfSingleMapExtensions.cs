using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
