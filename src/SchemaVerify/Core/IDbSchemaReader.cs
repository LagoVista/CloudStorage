using System.Data.Common;

namespace SchemaVerify.Core;

public interface IDbSchemaReader
{
    Task<SchemaModel> ReadAsync(DbConnection connection, CancellationToken ct = default);
}

public interface IDbSchemaReaderFactory
{
    IDbSchemaReader Create(string provider);
    DbConnection CreateConnection(string provider, string connectionString);
}
