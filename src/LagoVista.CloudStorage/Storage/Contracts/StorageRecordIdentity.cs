using System;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Canonical physical identity conventions for mutable record storage.
    /// A CLR record type always maps to the same collection name and common field paths.
    /// Callers do not supply or override these values during CRUD operations.
    /// </summary>
    public static class StorageRecordIdentity
    {
        public const string IdPath = nameof(IApplicationDataRecord.Id);
        public const string OrganizationIdPath = nameof(IApplicationDataRecord.Organization) + ".Id";
        public const string CreationDatePath = nameof(IApplicationDataRecord.CreationDate);
        public const string LastUpdatedDatePath = nameof(IApplicationDataRecord.LastUpdatedDate);

        public static string GetCollectionName<TRecord>()
        {
            return GetCollectionName(typeof(TRecord));
        }

        public static string GetCollectionName(Type recordType)
        {
            if (recordType == null) throw new ArgumentNullException(nameof(recordType));
            if (String.IsNullOrWhiteSpace(recordType.Name)) throw new ArgumentException("Record type must have a name.", nameof(recordType));

            return recordType.Name;
        }
    }
}
