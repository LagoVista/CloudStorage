using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityListCacheInvalidator
    {
        Task InvalidateAsync<TEntity>(string orgId);
        Task InvalidateAsync(string orgId, Type entityType);
        Task InvalidateAsync(string orgId, string entityType);
    }
}
