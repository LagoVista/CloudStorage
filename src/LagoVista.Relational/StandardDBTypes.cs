using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public static class StandardDBTypes
    {
        public const string EncryptionStorage = "varchar(1024)";
        public const string NormalizedId32Storage = "varchar(32)";
        public const string UtcTimestampStorage = "datetime7(2)";
        public const string UuidStorage = "uniqueidentifier";
        public const string CalendarDateStorage = "date";
        public const string TextMax = "varchar(max)";
        public const string TextLong = "varchar(2048)";
        public const string TextMedium = "varchar(2048)";
        public const string TextShort = "varchar(128)";
        public const string FlagStorage = "bit";    
    }
}
