using System;
using System.Collections.Generic;

namespace Relational.Tests.Core.Utils
{
    /// <summary>
    /// Options for reflection-based comparison.
    ///
    /// Designed to be strict by default, but with escape hatches:
    /// - Ignore properties by name
    /// - Ignore properties marked with specific attribute type names
    /// - Provide explicit source->target name mappings when properties are not 1:1
    /// - Add custom comparers for specific types
    ///
    /// IMPORTANT MODES:
    /// - Round-trip repo validation should be SAME-SHAPE + SAME-NAME by default (no attribute mapping).
    /// - Mapper validation may opt into attribute-driven mapping behavior.
    /// </summary>
    public sealed class ReflectionCompareOptions
    {
        public HashSet<string> IgnoreSourceProperties { get; } = new(StringComparer.Ordinal);
        public HashSet<string> IgnoreTargetProperties { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Attribute type names to ignore when present on the SOURCE property.
        /// Example: "IgnoreOnMapToAttribute".
        /// </summary>
        public HashSet<string> IgnoreSourceAttributeTypeNames { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Attribute type names to ignore when present on the TARGET property.
        /// Example: "IgnoreOnMapToAttribute".
        /// </summary>
        public HashSet<string> IgnoreTargetAttributeTypeNames { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Manual mapping from source property name -> target property name.
        /// Useful when properties are not 1:1 and you do not want attribute-driven mapping.
        /// </summary>
        public Dictionary<string, string> NameMap { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// When true, if a mapped target property cannot be found, the comparer will report a mismatch.
        /// When false, missing target properties are skipped.
        /// Default: true (strict).
        /// </summary>
        public bool FailIfTargetPropertyMissing { get; set; } = true;

        /// <summary>
        /// When true, string null and empty are treated as equal.
        /// Default: false (strict).
        /// </summary>
        public bool TreatNullAndEmptyStringAsEqual { get; set; } = false;

        /// <summary>
        /// When true, compares EntityHeader.Text as well as Id (where applicable).
        /// Default: false (compare Id only).
        /// </summary>
        public bool CompareEntityHeaderText { get; set; } = false;

        /// <summary>
        /// When true, the comparer is allowed to use attribute-driven mapping conventions
        /// (MapToAttribute, MapToNameAndIdAttribute, EncryptedFieldAttribute, etc.) to resolve target names.
        ///
        /// For repo round-trip tests (model->repo->model), this should be false so that the public
        /// contract is strictly "same shape and same names".
        ///
        /// Default: true (mapping-aware).
        /// </summary>
        public bool UseMappingAttributes { get; set; } = true;

        /// <summary>
        /// When true, enforces that the set of public readable properties on source and target match
        /// exactly (after applying ignore lists and ignore-attribute filters).
        ///
        /// This is ideal for repo round-trip tests where the contract is "same shape".
        /// Default: false.
        /// </summary>
        public bool RequireSameShape { get; set; } = false;

        /// <summary>
        /// When true, for encrypted-field comparisons (only relevant when UseMappingAttributes=true),
        /// the comparer will use the relaxed behavior (e.g., target encrypted string must be non-empty)
        /// instead of comparing plaintext values.
        ///
        /// For repo round-trip tests (model->repo->model), you generally want UseMappingAttributes=false,
        /// so this should not come into play.
        /// Default: true.
        /// </summary>
        public bool AllowEncryptedFieldRelaxation { get; set; } = true;

        /// <summary>
        /// Optional custom comparer by type.
        /// If present, it is used for that type.
        /// </summary>
        internal Dictionary<Type, Func<object, object, bool>> CustomComparers { get; } = new();

        public ReflectionCompareOptions Ignore(params string[] sourcePropertyNames)
        {
            for (var i = 0; i < sourcePropertyNames.Length; i++)
                IgnoreSourceProperties.Add(sourcePropertyNames[i]);

            return this;
        }

        public ReflectionCompareOptions IgnoreTarget(params string[] targetPropertyNames)
        {
            for (var i = 0; i < targetPropertyNames.Length; i++)
                IgnoreTargetProperties.Add(targetPropertyNames[i]);

            return this;
        }

        public ReflectionCompareOptions IgnoreSourceAttribute(params string[] attributeTypeNames)
        {
            for (var i = 0; i < attributeTypeNames.Length; i++)
                IgnoreSourceAttributeTypeNames.Add(attributeTypeNames[i]);

            return this;
        }

        public ReflectionCompareOptions IgnoreTargetAttribute(params string[] attributeTypeNames)
        {
            for (var i = 0; i < attributeTypeNames.Length; i++)
                IgnoreTargetAttributeTypeNames.Add(attributeTypeNames[i]);

            return this;
        }

        public ReflectionCompareOptions Map(string sourcePropertyName, string targetPropertyName)
        {
            NameMap[sourcePropertyName] = targetPropertyName;
            return this;
        }

        public ReflectionCompareOptions WithComparer<T>(Func<T, T, bool> comparer)
        {
            CustomComparers[typeof(T)] = (a, b) => comparer((T)a, (T)b);
            return this;
        }

        /// <summary>
        /// Default strict behavior for mapping-aware comparisons (source->dto, dto->model, etc.).
        /// Keeps ignore rules for IgnoreOnMapToAttribute, and allows attribute-driven mapping.
        /// </summary>
        public static ReflectionCompareOptions StrictDefaults()
            => new ReflectionCompareOptions()
                .IgnoreSourceAttribute("IgnoreOnMapToAttribute")
                .IgnoreTargetAttribute("IgnoreOnMapToAttribute");

        /// <summary>
        /// Repo round-trip defaults:
        /// - SAME SHAPE + SAME NAME comparisons
        /// - No attribute-driven mapping assistance
        /// - Enforce shape equality unless you override RequireSameShape=false
        /// - Still respects IgnoreOnMapToAttribute by default, but you can remove those ignores if you want
        ///   the contract to include those properties too.
        /// </summary>
        public static ReflectionCompareOptions RoundTripDefaults()
            => new ReflectionCompareOptions()
                .IgnoreSourceAttribute("IgnoreOnMapToAttribute")
                .IgnoreTargetAttribute("IgnoreOnMapToAttribute")
                .RoundTripStrict();

        /// <summary>
        /// Fluent helper: configure this instance for strict repo round-trip comparisons.
        /// </summary>
        public ReflectionCompareOptions RoundTripStrict()
        {
            UseMappingAttributes = false;
            RequireSameShape = true;
            AllowEncryptedFieldRelaxation = false;
            return this;
        }

        /// <summary>
        /// Fluent helper: configure this instance for mapping-aware comparisons.
        /// </summary>
        public ReflectionCompareOptions MappingAware()
        {
            UseMappingAttributes = true;
            RequireSameShape = false;
            AllowEncryptedFieldRelaxation = true;
            return this;
        }
    }
}