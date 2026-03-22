using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generated.Vendor
{
    // generated: full source query and reader
    public static class VendorSourceReader
    {
        public const string SourceQuery = @"
SELECT
    Id,
    OrganizationId,
    DefaultExpenseCategoryId,
    Name,
    [Key],
    Description,
    MaxAmount,
    PayPeriod,
    Notes,
    Contact,
    Phone,
    Icon,
    Address1,
    Address2,
    City,
    StateOrProvince,
    PostalCode,
    Country,
    CreatedById,
    LastUpdatedById,
    CreationDate,
    LastUpdateDate,
    IsActive,
    DefaultAccountTransactionCategoryId
FROM dbo.Vendor
ORDER BY Id;";

        public static async Task<List<SourceVendorRow>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            var rows = new List<SourceVendorRow>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = new SqlCommand(SourceQuery, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                rows.Add(new SourceVendorRow
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    OrganizationId = reader.IsDBNull(reader.GetOrdinal("OrganizationId")) ? null : reader["OrganizationId"].ToString(),
                    DefaultExpenseCategoryId = reader.GetGuid(reader.GetOrdinal("DefaultExpenseCategoryId")),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader["Name"].ToString(),
                    Key = reader.IsDBNull(reader.GetOrdinal("Key")) ? null : reader["Key"].ToString(),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader["Description"].ToString(),
                    MaxAmount = reader.GetDecimal(reader.GetOrdinal("MaxAmount")),
                    PayPeriod = reader.IsDBNull(reader.GetOrdinal("PayPeriod")) ? null : reader["PayPeriod"].ToString(),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader["Notes"].ToString(),
                    Contact = reader.IsDBNull(reader.GetOrdinal("Contact")) ? null : reader["Contact"].ToString(),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader["Phone"].ToString(),
                    Icon = reader.IsDBNull(reader.GetOrdinal("Icon")) ? null : reader["Icon"].ToString(),
                    Address1 = reader.IsDBNull(reader.GetOrdinal("Address1")) ? null : reader["Address1"].ToString(),
                    Address2 = reader.IsDBNull(reader.GetOrdinal("Address2")) ? null : reader["Address2"].ToString(),
                    City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader["City"].ToString(),
                    StateOrProvince = reader.IsDBNull(reader.GetOrdinal("StateOrProvince")) ? null : reader["StateOrProvince"].ToString(),
                    PostalCode = reader.IsDBNull(reader.GetOrdinal("PostalCode")) ? null : reader["PostalCode"].ToString(),
                    Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader["Country"].ToString(),
                    CreatedById = reader.IsDBNull(reader.GetOrdinal("CreatedById")) ? null : reader["CreatedById"].ToString(),
                    LastUpdatedById = reader.IsDBNull(reader.GetOrdinal("LastUpdatedById")) ? null : reader["LastUpdatedById"].ToString(),
                    CreationDate = reader.GetDateTime(reader.GetOrdinal("CreationDate")),
                    LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    DefaultAccountTransactionCategoryId = reader.IsDBNull(reader.GetOrdinal("DefaultAccountTransactionCategoryId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("DefaultAccountTransactionCategoryId")),
                });
            }

            return rows;
        }
    }
}
