using System;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class ApplicationDataConcurrencyToken : IEquatable<ApplicationDataConcurrencyToken>
    {
        internal ApplicationDataConcurrencyToken(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Concurrency token value is required.", nameof(value));

            Value = value;
        }

        public string Value { get; }

        public bool Equals(ApplicationDataConcurrencyToken other)
        {
            return other != null && String.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ApplicationDataConcurrencyToken);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public sealed class VersionedApplicationDataRecord<TRecord>
        where TRecord : class, IApplicationDataRecord
    {
        internal VersionedApplicationDataRecord(TRecord record, ApplicationDataConcurrencyToken concurrencyToken)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            ConcurrencyToken = concurrencyToken ?? throw new ArgumentNullException(nameof(concurrencyToken));
        }

        public TRecord Record { get; }
        public ApplicationDataConcurrencyToken ConcurrencyToken { get; }
    }

    public enum ApplicationDataMutationStatus
    {
        Updated,
        Conflict,
        NotFound
    }

    public sealed class ApplicationDataMutationResult
    {
        internal ApplicationDataMutationResult(ApplicationDataMutationStatus status, ApplicationDataConcurrencyToken concurrencyToken = null)
        {
            Status = status;
            ConcurrencyToken = concurrencyToken;
        }

        public ApplicationDataMutationStatus Status { get; }
        public ApplicationDataConcurrencyToken ConcurrencyToken { get; }
    }
}
