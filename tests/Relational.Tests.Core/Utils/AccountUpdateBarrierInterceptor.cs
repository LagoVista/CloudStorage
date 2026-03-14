using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class AccountUpdateBarrierInterceptor : DbCommandInterceptor
{
    private int _hits;
    private readonly TaskCompletionSource _firstHit = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _secondHit = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitForFirstHitAsync() => _firstHit.Task;

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (IsAccountUpdate(command.CommandText))
        {
            var hit = Interlocked.Increment(ref _hits);
            if (hit == 1)
            {
                _firstHit.TrySetResult();
                await _secondHit.Task.ConfigureAwait(false);
            }
            else if (hit == 2)
            {
                _secondHit.TrySetResult();
            }
        }

        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsAccountUpdate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;

        // Works for Postgres logs you showed; for SQL Server/MySQL you may expand patterns later.
        return sql.Contains("UPDATE \"Account\"") && sql.Contains("\"Version\"");
    }
}