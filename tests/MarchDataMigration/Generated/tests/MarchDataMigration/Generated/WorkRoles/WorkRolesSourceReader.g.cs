using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.WorkRoles
{
    // generated: full source query and reader
    public static class WorkRolesSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    OrganizationId,
    [Key],
    Name,
    Icon,
    IsActive,
    Description,
    CreationDate,
    CreatedById,
    LastUpdateDate,
    LastUpdatedById
FROM dbo.WorkRoles
ORDER BY Id;";

        public static async Task<List<SourceWorkRolesRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceWorkRolesRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceWorkRolesRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Icon = reader.IsDBNull(reader.GetOrdinal("Icon")) ? null : reader["Icon"].ToString(),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                });
            }

            return rows;
        }
    }
}
