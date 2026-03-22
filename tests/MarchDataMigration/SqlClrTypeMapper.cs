// ================================
// File: LegacyMigrationScaffolding/Generation/SqlClrTypeMapper.cs
// ================================
using LegacyMigrationScaffolding.Schema;

namespace LegacyMigrationScaffolding.Generation
{
    public static class SqlClrTypeMapper
    {
        public static string MapToClrType(SchemaColumnInfo column)
        {
            var sqlType = column.SqlDataType.ToLowerInvariant();

            string clrType = sqlType switch
            {
                "uniqueidentifier" => "System.Guid",
                "int" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "decimal" => "decimal",
                "numeric" => "decimal",
                "money" => "decimal",
                "smallmoney" => "decimal",
                "float" => "double",
                "real" => "float",
                "date" => "System.DateTime",
                "datetime" => "System.DateTime",
                "datetime2" => "System.DateTime",
                "smalldatetime" => "System.DateTime",
                "datetimeoffset" => "System.DateTimeOffset",
                "time" => "System.TimeSpan",
                "char" => "string",
                "nchar" => "string",
                "varchar" => "string",
                "nvarchar" => "string",
                "text" => "string",
                "ntext" => "string",
                "xml" => "string",
                "binary" => "byte[]",
                "varbinary" => "byte[]",
                "image" => "byte[]",
                _ => "string"
            };

            if (clrType == "string" || clrType == "byte[]")
                return clrType;

            if (column.IsNullable)
                return $"{clrType}?";

            return clrType;
        }
    }
}