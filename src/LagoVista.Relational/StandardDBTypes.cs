using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public static class StandardDBTypes
    {
        public static string EncryptionStorage(string provider) => "varchar(1024)";
        public static string NormalizedId32Storage(string provider) => "varchar(32)";
        public static string UtcTimestampStorage(string provider) => "datetime2(7)";
        public static string UuidStorage(string provider) => "uniqueidentifier";
        public static string CalendarDateStorage(string provider) => "date";
        public static string TextMax(string provider) => "nvarchar(max)";
        public static string UrlStorage(string provider) => "nvarchar(max)";
        public static string HtmlStorage(string provider) => "nvarchar(max)";
        public static string TextLong(string provider) => "nvarchar(2048)";
        public static string TextMedium(string provider) => "nvarchar(1024)";
        public static string TextShort(string provider) => "nvarchar(128)";
        public static string TextTiny(string provider) => "nvarchar(50)";
        public static string FlagStorage(string provider) => "bit";
        public static string IntStorage(string provider) => "int";
        public static string DecimalStorage(string provider) => "decimal(18,2)";
        public static string DecimalMedium(string provider) => "decimal(9,2)";
        public static string DecimalSmall(string provider) => "decimal(5,2)";
        public static string LongStorage(string provider) => "bigint";
        public static string IconStorage(string provider) => "varchar(1024)";
        public static string NameStorage(string provider) => "nvarchar(255)";
        public static string KeyStorage(string provider) => "varchar(64)";
        public static string MoneyStorage(string provider) => "decimal(18,2)";
        public static string StatusStorage(string provider) => "nvarchar(50)";
        public static string CategoryStorage(string provider) => "nvarchar(50)";
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
