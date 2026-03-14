using LagoVista.Core.Interfaces.Crypto;
using LagoVista.Core.Models;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.Relational.Services
{
    public class KeyIdTargetResolver : IKeyIdTargetResolver
    {
        readonly BillingDataContext _ctx;    // scoped cache: (targetPath|fkGuidN) -> resolved Guid
        private readonly ConcurrentDictionary<string, Task<Guid>> _cache =
            new ConcurrentDictionary<string, Task<Guid>>(StringComparer.Ordinal);

        public KeyIdTargetResolver(BillingDataContext ctx)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        public Task<Guid> ResolveIdAsync(string targetPath, Guid fkValue, EntityHeader org, EntityHeader user, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(targetPath)) throw new ArgumentNullException(nameof(targetPath));
            if (fkValue == Guid.Empty) throw new InvalidOperationException("fkValue is Guid.Empty.");
            if (org == null) throw new ArgumentNullException(nameof(org));
            if (user == null) throw new ArgumentNullException(nameof(user));

            var cacheKey = $"{targetPath}|{fkValue:N}";
            return _cache.GetOrAdd(cacheKey, _ => ResolveCoreAsync(targetPath, fkValue, ct));
        }

        private async Task<Guid> ResolveCoreAsync(string targetPath, Guid fkValue, CancellationToken ct)
        {
            // Allowlist + explicit queries (still “generic interface”, explicit implementation).
            switch (targetPath)
            {
                case "Invoice.CustomerId":
                    {
                        // SELECT CustomerId FROM Invoice WHERE Id = @p1
                        var customerId = await _ctx.Invoices
                            .AsNoTracking()
                            .Where(i => i.Id == fkValue)
                            .Select(i => i.CustomerId)
                            .SingleOrDefaultAsync(ct)
                            .ConfigureAwait(false);

                        if (customerId == Guid.Empty)
                            throw new InvalidOperationException($"Could not resolve CustomerId from Invoice.Id='{fkValue}'. (Not found or empty CustomerId)");

                        return customerId;
                    }

                case "Agreement.CustomerId":
                    {
                        // SELECT CustomerId FROM AgreementName WHERE Id = @p1
                        var customerId = await _ctx.Agreements
                            .AsNoTracking()
                            .Where(a => a.Id == fkValue)
                            .Select(a => a.CustomerId)
                            .SingleOrDefaultAsync(ct)
                            .ConfigureAwait(false);

                        if (customerId == Guid.Empty)
                            throw new InvalidOperationException($"Could not resolve CustomerId from Agreement.Id='{fkValue}'. (Not found or empty CustomerId)");

                        return customerId;
                    }

                default:
                    throw new InvalidOperationException($"Unsupported targetPath '{targetPath}'. Allowed: 'Invoice.CustomerId', 'Agreement.CustomerId'.");
            }
        }
    }
}
