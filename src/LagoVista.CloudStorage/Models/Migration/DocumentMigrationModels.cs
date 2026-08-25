using LagoVista.CloudStorage.DocumentDB;
using System.Collections.Generic;

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
        public IReadOnlyCollection<string> ExcludedEntityTypes { get; set; }
    }

    public sealed class DocumentMigrationRouteStatistics
    {
        public string EntityType { get; set; }
        public string CollectionName { get; set; }
        public int Read { get; set; }
        public int Written { get; set; }
        public int Excluded { get; set; }
        public int Failed { get; set; }
        public int UnresolvedRoute { get; set; }
    }

    public sealed class CosmosToMongoMigrationResult
    {
        public int PagesRead { get; set; }
        public int DocumentsRead { get; set; }
        public int DocumentsWritten { get; set; }
        public int DocumentsExcluded { get; set; }
        public int DocumentsSkipped { get; set; }
        public int DocumentsFailed { get; set; }
        public int UnresolvedRoutes { get; set; }
        public string ContinuationToken { get; set; }
        public bool Completed { get; set; }
        public bool DryRun { get; set; }
        public List<DocumentMigrationRouteStatistics> Routes { get; set; } = new List<DocumentMigrationRouteStatistics>();
    }

    public sealed class DocumentMigrationValidationStatistics
    {
        public string EntityType { get; set; }
        public string CollectionName { get; set; }
        public long SourceCount { get; set; }
        public long DestinationCount { get; set; }
        public bool Matches => SourceCount == DestinationCount;
    }

    public sealed class CosmosToMongoValidationResult
    {
        public long SourceCount { get; set; }
        public long DestinationCount { get; set; }
        public bool Matches { get; set; }
        public List<DocumentMigrationValidationStatistics> Routes { get; set; } = new List<DocumentMigrationValidationStatistics>();
    }
}
