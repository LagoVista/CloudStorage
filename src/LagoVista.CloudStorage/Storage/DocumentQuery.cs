using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.DocumentDB
{
    /// <summary>
    /// Identifies provider-specific query shapes that cannot be expressed cleanly through the
    /// normal document CRUD/filter API. Callers choose a known query and providers translate
    /// it into Cosmos SQL or Mongo query/pipeline syntax.
    /// </summary>
    public enum KnownDocumentQuery
    {
        EntityVideoCompositionSourcesByTypeAndOrganization,
        CustomerIndustryNicheSalesStageCounts,
        WorkTaskKanban,
        EntityPreparationCandidateById,
        EntityPreparationCandidatesByType,
        IncompleteEntityPreparationCandidatesByType,

        /// <summary>Returns projected list items using the standard entity-list filters, sort, and paging.</summary>
        EntityListItems,

        /// <summary>Returns entity headers using the standard entity-list filters, sort, and paging.</summary>
        EntityListHeaders,

        /// <summary>Returns distinct category headers visible to the organization under the standard entity-list filters.</summary>
        EntityListCategories
    }

    public sealed class KnownDocumentQueryParameter
    {
        public KnownDocumentQueryParameter(string name, object value)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A known document query parameter name is required.", nameof(name));

            Name = name.Trim().TrimStart('@');
            Value = value;
        }

        public string Name { get; }
        public object Value { get; }
    }

    public sealed class KnownDocumentQueryRequest
    {
        private readonly Dictionary<string, object> _parameters =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public KnownDocumentQueryRequest(KnownDocumentQuery query)
        {
            Query = query;
        }

        public KnownDocumentQuery Query { get; }

        public IReadOnlyDictionary<string, object> Parameters => _parameters;

        public KnownDocumentQueryRequest WithParameter(string name, object value)
        {
            var parameter = new KnownDocumentQueryParameter(name, value);
            _parameters[parameter.Name] = parameter.Value;
            return this;
        }

        public T GetRequired<T>(string name)
        {
            var normalizedName = NormalizeName(name);
            if (!_parameters.TryGetValue(normalizedName, out var value))
                throw new InvalidOperationException($"Known document query '{Query}' requires parameter '{normalizedName}'.");

            if (value is T typedValue)
                return typedValue;

            throw new InvalidOperationException(
                $"Known document query '{Query}' parameter '{normalizedName}' expected {typeof(T).Name} but received {value?.GetType().Name ?? "null"}.");
        }

        private static string NormalizeName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A known document query parameter name is required.", nameof(name));

            return name.Trim().TrimStart('@');
        }
    }

    /// <summary>
    /// Provider-neutral equality filter for the common raw-document query path used by shared
    /// utilities that operate on runtime entity types.
    /// </summary>
    public sealed class DocumentFilterRequest
    {
        private readonly Dictionary<string, object> _equals =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, object> Equals => _equals;
        public string SortField { get; private set; }
        public bool SortDescending { get; private set; }
        public int? Limit { get; private set; }

        public DocumentFilterRequest WhereEquals(string fieldName, object value)
        {
            if (String.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("A document field name is required.", nameof(fieldName));

            _equals[fieldName.Trim()] = value;
            return this;
        }

        public DocumentFilterRequest OrderBy(string fieldName, bool descending = false)
        {
            if (String.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("A document sort field is required.", nameof(fieldName));

            SortField = fieldName.Trim();
            SortDescending = descending;
            return this;
        }

        public DocumentFilterRequest Take(int limit)
        {
            if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit));
            Limit = limit;
            return this;
        }
    }
}
