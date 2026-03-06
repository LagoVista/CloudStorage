using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public static class StandardDBTypes
    {
        public static string EncryptionStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(1024)",
            _ => "varchar(1024)"
        };

        public static string NormalizedId32Storage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(32)",
            _ => "varchar(32)"
        };

        public static string UtcTimestampStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "timestamp with time zone",
            _ => "datetime2(7)"
        };

        public static string UuidStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "uuid",
            _ => "uniqueidentifier"
        };

        public static string CalendarDateStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "date",
            _ => "date"
        };

        public static string TextMax(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "text",
            _ => "nvarchar(max)"
        };

        public static string UrlStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "text",
            _ => "nvarchar(max)"
        };

        public static string HtmlStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "text",
            _ => "nvarchar(max)"
        };

        public static string TextLong(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(2048)",
            _ => "nvarchar(2048)"
        };

        public static string TextMedium(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(1024)",
            _ => "nvarchar(1024)"
        };

        public static string TextShort(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(128)",
            _ => "nvarchar(128)"
        };

        public static string TextTiny(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(50)",
            _ => "nvarchar(50)"
        };

        public static string FlagStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "INTEGER",
            ModelBuilderProviderExtensions.Postgres => "boolean",
            _ => "bit"
        };

        public static string IntStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "INTEGER",
            ModelBuilderProviderExtensions.Postgres => "integer",
            _ => "int"
        };

        public static string DecimalStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "numeric(18,2)",
            _ => "decimal(18,2)"
        };

        public static string DecimalMedium(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "numeric(9,2)",
            _ => "decimal(9,2)"
        };

        public static string DecimalSmall(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "numeric(5,2)",
            _ => "decimal(5,2)"
        };

        public static string LongStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "INTEGER",
            ModelBuilderProviderExtensions.Postgres => "bigint",
            _ => "bigint"
        };

        public static string IconStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(1024)",
            _ => "varchar(1024)"
        };

        public static string NameStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(255)",
            _ => "nvarchar(255)"
        };

        public static string KeyStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(64)",
            _ => "varchar(64)"
        };

        public static string MoneyStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "numeric(18,2)",
            _ => "decimal(18,2)"
        };

        public static string StatusStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(50)",
            _ => "nvarchar(50)"
        };

        public static string CategoryStorage(string provider) => provider switch
        {
            ModelBuilderProviderExtensions.Sqlite => "TEXT",
            ModelBuilderProviderExtensions.Postgres => "varchar(50)",
            _ => "nvarchar(50)"
        };
    }
    public static class StandardDbDefaults
    {
        public static string False(string provider) => "0";
        public static string True(string provider) => "1";
        public static string NewGuid(string provider) => "NEWID()"; 
        public static string Text(string provider, string value) => $"'{value}'";

        public static string None(string provider) => "none";

        public static string Zero(string provider) => "0";
        public static string One(string provider) => "1";
    }
}
