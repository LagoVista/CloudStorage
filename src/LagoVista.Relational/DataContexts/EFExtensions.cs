using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace LagoVista
{
    public static class EFExtensions
    {
        public static void LowerCaseNames(this ModelBuilder modelBuilder, string dbName)
        {
            if (!dbName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
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

    public static class ModelBuilderProviderExtensions
    {
        private const string ProviderKey = "App:ProviderName";

        private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string Sqlite = "Microsoft.EntityFrameworkCore.Sqlite";
        private const string Postgres = "Npgsql.EntityFrameworkCore.PostgreSQL";
        private const string PomeloMySql = "Pomelo.EntityFrameworkCore.MySql";
        private const string OracleMySql = "MySql.EntityFrameworkCore";

        public static ModelBuilder SeedProviderName(this ModelBuilder modelBuilder, string? providerName)
        {
            // Store it on the model so we can query it later via ModelBuilder.
            modelBuilder.HasAnnotation(ProviderKey, providerName);
            return modelBuilder;
        }

        public static string? GetProviderName(this ModelBuilder modelBuilder)
            => modelBuilder.Model.FindAnnotation(ProviderKey)?.Value?.ToString()
               ?? modelBuilder.Model.FindAnnotation("Relational:ProviderName")?.Value?.ToString();

        public static bool IsSqlServer(this ModelBuilder modelBuilder)
            => modelBuilder.GetProviderName() == SqlServer;

        public static bool IsSqlite(this ModelBuilder modelBuilder)
            => modelBuilder.GetProviderName() == Sqlite;

        public static bool IsPostgres(this ModelBuilder modelBuilder)
            => modelBuilder.GetProviderName() == Postgres;

        public static bool IsMySql(this ModelBuilder modelBuilder)
            => modelBuilder.GetProviderName() == PomeloMySql
               || modelBuilder.GetProviderName() == OracleMySql;
    }
}
