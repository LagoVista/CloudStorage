using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.DocumentDB
{
    /// <summary>
    /// Identifies document query shapes that cannot be expressed cleanly through the
    /// normal expression-based repository API. Application code must not provide raw
    /// Cosmos SQL (or Mongo query syntax) directly. Provider implementations translate
    /// these semantic query types into their native query representation.
    /// </summary>
    public enum DocumentQueryType
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
        EntityListCategories,

        /// <summary>Returns raw entities of one type owned by an organization, ordered by name.</summary>
        EntityUtilsDocumentsByType,

        /// <summary>Returns one raw entity document by ID.</summary>
        EntityUtilsDocumentById,

        /// <summary>Counts entities of one type owned by an organization.</summary>
        EntityUtilsCountByType,

        /// <summary>Returns entities whose Status.Id is missing/null or matches one of the requested status ids.</summary>
        EntityUtilsDocumentsByStatusIds,

        /// <summary>Returns entities where a named field is missing, null, or an empty string.</summary>
        EntityUtilsDocumentsWithEmptyField,

        /// <summary>Returns document ids where a named field equals the requested value.</summary>
        EntityUtilsDocumentsByFieldValue,

        /// <summary>Returns entities where every requested checklist step has a LastRun.</summary>
        EntityUtilsCompletedChecklistCandidates,

        /// <summary>Counts entities where every requested checklist step has a LastRun.</summary>
        EntityUtilsCompletedChecklistCount,

        /// <summary>Returns entities where all prerequisite steps have a LastRun and at least one target step does not.</summary>
        EntityUtilsReadyChecklistCandidates,

        /// <summary>Counts entities where all prerequisite steps have a LastRun and at least one target step does not.</summary>
        EntityUtilsReadyChecklistCount,

        /// <summary>Returns entities where at least one required prerequisite step does not have a LastRun.</summary>
        EntityUtilsBlockedChecklistCandidates,
    }

    public sealed class DocumentCountResult
    {
        public int Count { get; set; }
    }

    public sealed class DocumentQueryParameter
    {
        public DocumentQueryParameter(string name, object value)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A document query parameter name is required.", nameof(name));

            Name = name.Trim().TrimStart('@');
            Value = value;
        }

        public string Name { get; }
        public object Value { get; }
    }

    public sealed class DocumentQueryRequest
    {
        private readonly Dictionary<string, object> _parameters =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public DocumentQueryRequest(DocumentQueryType queryType)
        {
            QueryType = queryType;
        }

        public DocumentQueryType QueryType { get; }

        public IReadOnlyDictionary<string, object> Parameters => _parameters;

        public DocumentQueryRequest WithParameter(string name, object value)
        {
            var parameter = new DocumentQueryParameter(name, value);
            _parameters[parameter.Name] = parameter.Value;
            return this;
        }

        public T GetRequired<T>(string name)
        {
            var normalizedName = NormalizeName(name);
            if (!_parameters.TryGetValue(normalizedName, out var value))
                throw new InvalidOperationException($"Registered document query '{QueryType}' requires parameter '{normalizedName}'.");

            if (value is T typedValue)
                return typedValue;

            throw new InvalidOperationException(
                $"Registered document query '{QueryType}' parameter '{normalizedName}' expected {typeof(T).Name} but received {value?.GetType().Name ?? "null"}.");
        }

        private static string NormalizeName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A document query parameter name is required.", nameof(name));

            return name.Trim().TrimStart('@');
        }
    }
}
