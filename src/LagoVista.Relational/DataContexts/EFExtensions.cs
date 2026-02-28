using Microsoft.EntityFrameworkCore;
using System;

namespace LagoVista.Relational.DataContexts
{
    public static class EFExtensions
    {
        public static void LowerCaseNames(this ModelBuilder modelBuilder, string dbName)
        {
            if(!dbName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Table name
                var tableName = entity.GetTableName();
                if (!string.IsNullOrEmpty(tableName))
                {
                    entity.SetTableName(tableName.ToLowerInvariant());
                }

                // Schema
                var schema = entity.GetSchema();
                if (!string.IsNullOrEmpty(schema))
                {
                    entity.SetSchema(schema.ToLowerInvariant());
                }

                // Columns
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.Name.ToLowerInvariant());
                }

                // Primary & alternate keys
                foreach (var key in entity.GetKeys())
                {
                    var keyName = key.GetName();
                    if (!string.IsNullOrEmpty(keyName))
                    {
                        key.SetName(keyName.ToLowerInvariant());
                    }
                }

                // Foreign keys
                foreach (var fk in entity.GetForeignKeys())
                {
                    var constraintName = fk.GetConstraintName();
                    if (!string.IsNullOrEmpty(constraintName))
                    {
                        fk.SetConstraintName(constraintName.ToLowerInvariant());
                    }
                }

                // Indexes
                foreach (var index in entity.GetIndexes())
                {
                    var indexName = index.GetDatabaseName();
                    if (!string.IsNullOrEmpty(indexName))
                    {
                        index.SetDatabaseName(indexName.ToLowerInvariant());
                    }
                }
            }
        }
    }

}
