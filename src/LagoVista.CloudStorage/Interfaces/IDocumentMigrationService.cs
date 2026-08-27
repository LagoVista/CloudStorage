using LagoVista.CloudStorage.Models.Migration;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IDocumentMigrationService
    {
        Task<CosmosToMongoMigrationResult> MigrateCosmosToMongoAsync(CosmosToMongoMigrationRequest request, CancellationToken cancellationToken = default);
        Task<CosmosToMongoValidationResult> ValidateCosmosToMongoAsync(CosmosToMongoMigrationRequest request, CancellationToken cancellationToken = default);
    }
}
