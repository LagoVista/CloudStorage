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
        /// <summary>
        /// Projects video-composition source fields for one entity type and organization.
        /// </summary>
        EntityVideoCompositionSourcesByTypeAndOrganization,

        /// <summary>
        /// Aggregates customer counts by industry, niche, and sales stage for an organization.
        /// </summary>
        CustomerIndustryNicheSalesStageCounts,

        /// <summary>
        /// Resolves the dynamic work-task set used by the Kanban view.
        /// </summary>
        WorkTaskKanban,

        /// <summary>
        /// Returns one entity-preparation summary by entity type, entity ID, and organization.
        /// </summary>
        EntityPreparationCandidateById,

        /// <summary>
        /// Returns entity-preparation summaries by entity type and organization.
        /// </summary>
        EntityPreparationCandidatesByType,

        /// <summary>
        /// Returns entity-preparation summaries that are not production-ready.
        /// </summary>
        IncompleteEntityPreparationCandidatesByType
    }

    /// <summary>
    /// Provider-neutral named parameter supplied to a registered document query.
    /// Parameter names intentionally omit provider-specific prefix requirements.
    /// </summary>
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

    /// <summary>
    /// Provider-neutral request for one registered document query shape.
    /// </summary>
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
