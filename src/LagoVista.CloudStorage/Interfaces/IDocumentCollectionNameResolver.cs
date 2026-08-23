using System;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IDocumentCollectionNameResolver
    {
        string Resolve(string databaseName, Type entityType, string explicitCollectionName = null);
        bool TryResolve(string databaseName, string entityTypeName, out string collectionName);
        string GetFallback(string databaseName);
    }
}
