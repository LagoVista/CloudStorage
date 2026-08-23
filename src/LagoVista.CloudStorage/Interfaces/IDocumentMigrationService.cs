using LagoVista.CloudStorage.DocumentDB;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public sealed class CosmosToMongoMigrationRequest
    {
        public DocumentStorageSettings Source { get; set; }
        public MongoDocumentStorageSettings Target { get; set; }
        public string SourceCollectionName { get; set; }
        public string EntityType { get; set; }
        public string ContinuationToken { get; set; }
        public int BatchSize { get; set; } = 200;
        public int MaxPages { get; set; }
        public bool DryRun { get; set; }
    }

    public sealed class DocumentMigrationRouteStatistics
    {
        public string EntityType { get; set; }
        public string CollectionName { get; set; }
        public int Read { get; set; }
        public int Written { get; set; }
        public int Failed { get; set; }
        public int UnresolvedRoute { get; set; }
    }

    public sealed class CosmosToMongoMigrationResult
    {
        public int PagesRead { get; set; }
        public int DocumentsRead { get; set; }
        public int DocumentsWritten { get; set; }
        public int DocumentsSkipped { get; set; }
        public int DocumentsFailed { get; set; }
        public int UnresolvedRoutes { get; set; }
        public string ContinuationToken { get; set; }
        public bool Completed { get; set; }
        public bool DryRun { get; set; }
        public List<DocumentMigrationRouteStatistics> Routes { get; set; } = new List<DocumentMigrationRouteStatistics>();
    }

    public interface IDocumentMigrationService
    {
        Task<CosmosToMongoMigrationResult> MigrateCosmosToMongoAsync(CosmosToMongoMigrationRequest request, CancellationToken cancellationToken = default);
    }
}
