using System;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Declares the canonical storage identity for a mutable record when the CLR
    /// persistence type name intentionally differs from the physical collection name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class StorageRecordNameAttribute : Attribute
    {
        public StorageRecordNameAttribute(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("Storage record name is required.", nameof(name));
            Name = name.Trim();
        }

        public string Name { get; }
    }
}
