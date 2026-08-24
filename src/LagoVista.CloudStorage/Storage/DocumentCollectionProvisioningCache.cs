using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.DocumentDB
{
    public sealed class DocumentCollectionProvisioningCache
    {
        private readonly ConcurrentDictionary<string, Lazy<Task>> _verified = new ConcurrentDictionary<string, Lazy<Task>>(StringComparer.Ordinal);

        public async Task EnsureAsync(string key, Func<Task> ensureAction)
        {
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (ensureAction == null) throw new ArgumentNullException(nameof(ensureAction));

            var lazy = _verified.GetOrAdd(key, _ => new Lazy<Task>(ensureAction, true));
            try
            {
                await lazy.Value.ConfigureAwait(false);
            }
            catch
            {
                if (_verified.TryGetValue(key, out var current) && ReferenceEquals(current, lazy))
                    _verified.TryRemove(key, out _);
                throw;
            }
        }

        public bool IsVerified(string key)
        {
            if (String.IsNullOrWhiteSpace(key)) return false;
            return _verified.TryGetValue(key, out var lazy) && lazy.IsValueCreated && lazy.Value.Status == TaskStatus.RanToCompletion;
        }
    }
}
