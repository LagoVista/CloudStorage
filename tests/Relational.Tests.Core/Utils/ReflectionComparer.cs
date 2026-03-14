using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Relational.Tests.Core.Utils
{
    public sealed class ReflectionCompareResult
    {
        public List<string> Differences { get; } = new();
        public bool Success => Differences.Count == 0;

        public override string ToString()
        {
            if (Success)
                return "No differences.";

            return string.Join(Environment.NewLine, Differences);
        }
    }

    /// <summary>
    /// Reflection-based comparer for validating mapping/persistence without hand-writing asserts.
    ///
    /// IMPORTANT:
    /// - For repo round-trip validation (model in, model out), prefer ReflectionCompareOptions.RoundTripDefaults()
    ///   which enforces same-shape + same-name (no attribute-driven mapping).
    /// - For mapper validation (model -> dto, dto -> model), prefer ReflectionCompareOptions.StrictDefaults()
    ///   which can use attribute-driven mapping conventions.
    /// </summary>
    public static class ReflectionComparer
    {
        public static ReflectionCompareResult Compare(object source, object target, ReflectionCompareOptions options = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            options ??= ReflectionCompareOptions.StrictDefaults();

            var result = new ReflectionCompareResult();

            var sourceType = source.GetType();
            var targetType = target.GetType();

            var targetProps = targetType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

            var sourceProps = sourceType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);

            if (options.RequireSameShape)
                EnforceSameShape(source, target, sourceProps, targetProps, options, result);

            foreach (var sp in sourceProps)
            {
                if (options.IgnoreSourceProperties.Contains(sp.Name))
                    continue;

                if (HasAnyAttributeByName(sp, options.IgnoreSourceAttributeTypeNames))
                    continue;

                // Skip indexers
                if (sp.GetIndexParameters().Length > 0)
                    continue;

                var mapping = ResolveMapping(sp, options);
                if (mapping.Kind == PropertyMappingKind.Skip)
                    continue;

                if (mapping.Kind == PropertyMappingKind.EntityHeaderToNameAndId)
                {
                    CompareEntityHeaderToNameAndId(source, sp, target, targetProps, mapping, options, result);
                    continue;
                }

                var targetPropName = mapping.TargetPropertyName;

                if (options.IgnoreTargetProperties.Contains(targetPropName))
                    continue;

                if (!targetProps.TryGetValue(targetPropName, out var tp))
                {
                    if (options.FailIfTargetPropertyMissing)
                        result.Differences.Add($"Missing target property: {targetType.Name}.{targetPropName} (from source {sourceType.Name}.{sp.Name})");
                    continue;
                }

                if (HasAnyAttributeByName(tp, options.IgnoreTargetAttributeTypeNames))
                    continue;

                if (mapping.Kind == PropertyMappingKind.EncryptedField && options.AllowEncryptedFieldRelaxation)
                {
                    // Relaxed check: ensure encrypted target is non-empty.
                    var encryptedValue = tp.GetValue(target);
                    if (encryptedValue == null || (encryptedValue is string s && string.IsNullOrWhiteSpace(s)))
                        result.Differences.Add($"Encrypted target missing/empty: {targetType.Name}.{tp.Name} (from source {sourceType.Name}.{sp.Name})");

                    continue;
                }

                var sv = sp.GetValue(source);
                var tv = tp.GetValue(target);

                if (AreEqual(sv, tv, sp.PropertyType, tp.PropertyType, options))
                    continue;

                result.Differences.Add(
                    $"Mismatch: {sourceType.Name}.{sp.Name} -> {targetType.Name}.{tp.Name}. Expected='{FormatValue(sv)}' Actual='{FormatValue(tv)}'");
            }

            return result;
        }

        private static void EnforceSameShape(
            object source,
            object target,
            IEnumerable<PropertyInfo> sourceProps,
            Dictionary<string, PropertyInfo> targetProps,
            ReflectionCompareOptions options,
            ReflectionCompareResult result)
        {
            var sourceType = source.GetType();
            var targetType = target.GetType();

            // Build the set of target property names implied by the source properties (after mapping/ignore rules).
            var expectedTargetNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sp in sourceProps)
            {
                if (!sp.CanRead)
                    continue;

                if (sp.GetIndexParameters().Length > 0)
                    continue;

                if (options.IgnoreSourceProperties.Contains(sp.Name))
                    continue;

                if (HasAnyAttributeByName(sp, options.IgnoreSourceAttributeTypeNames))
                    continue;

                var mapping = ResolveMapping(sp, options);
                if (mapping.Kind == PropertyMappingKind.Skip)
                    continue;

                if (mapping.Kind == PropertyMappingKind.EntityHeaderToNameAndId)
                {
                    if (!options.IgnoreTargetProperties.Contains(mapping.TargetPropertyName))
                        expectedTargetNames.Add(mapping.TargetPropertyName);

                    if (mapping.TargetPropertyName2 != null && !options.IgnoreTargetProperties.Contains(mapping.TargetPropertyName2))
                        expectedTargetNames.Add(mapping.TargetPropertyName2);

                    continue;
                }

                if (!options.IgnoreTargetProperties.Contains(mapping.TargetPropertyName))
                    expectedTargetNames.Add(mapping.TargetPropertyName);
            }

            // Build the set of actual target property names participating in comparison (after ignore rules).
            var actualTargetNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tp in targetProps.Values)
            {
                if (!tp.CanRead)
                    continue;

                if (tp.GetIndexParameters().Length > 0)
                    continue;

                if (options.IgnoreTargetProperties.Contains(tp.Name))
                    continue;

                if (HasAnyAttributeByName(tp, options.IgnoreTargetAttributeTypeNames))
                    continue;

                actualTargetNames.Add(tp.Name);
            }

            // Missing on target
            foreach (var expected in expectedTargetNames)
            {
                if (!actualTargetNames.Contains(expected))
                    result.Differences.Add($"Shape mismatch: missing target property {targetType.Name}.{expected} (expected from {sourceType.Name})");
            }

            // Extra on target
            foreach (var actual in actualTargetNames)
            {
                if (!expectedTargetNames.Contains(actual))
                    result.Differences.Add($"Shape mismatch: extra target property {targetType.Name}.{actual} (not present on {sourceType.Name})");
            }
        }

        private static bool AreEqual(object sourceValue, object targetValue, Type sourceType, Type targetType, ReflectionCompareOptions options)
        {
            if (sourceValue == null && targetValue == null)
                return true;

            if (sourceValue == null || targetValue == null)
            {
                if (options.TreatNullAndEmptyStringAsEqual)
                {
                    if (sourceValue == null && targetValue is string ts && ts.Length == 0)
                        return true;
                    if (targetValue == null && sourceValue is string ss && ss.Length == 0)
                        return true;
                }

                return false;
            }

            // Custom comparers (keyed by declared sourceType, but fall back to runtime type if desired)
            if (options.CustomComparers.TryGetValue(sourceType, out var comparer))
                return comparer(sourceValue, targetValue);

            var runtimeSourceType = sourceValue.GetType();
            if (runtimeSourceType != sourceType && options.CustomComparers.TryGetValue(runtimeSourceType, out var comparer2))
                return comparer2(sourceValue, targetValue);

            // EntityHeader special-case (Id only by default)
            if (sourceValue is EntityHeader seh)
            {
                if (targetValue is EntityHeader teh)
                {
                    if (!String.Equals(seh.Id, teh.Id, StringComparison.Ordinal))
                        return false;

                    if (options.CompareEntityHeaderText && !String.Equals(seh.Text, teh.Text, StringComparison.Ordinal))
                        return false;

                    return true;
                }

                // EntityHeader -> Guid/string
                if (targetValue is Guid tg)
                    return TryParseGuid(seh.Id, out var sg) && sg == tg;

                if (targetValue is string ts)
                    return String.Equals(seh.Id, ts, StringComparison.Ordinal);
            }

            // Guid/string conversions
            if (sourceValue is Guid sguid && targetValue is string tstr)
                return String.Equals(sguid.ToString(), tstr, StringComparison.OrdinalIgnoreCase);

            if (sourceValue is string sstr && targetValue is Guid tguid)
                return TryParseGuid(sstr, out var sguid2) && sguid2 == tguid;

            // DateOnly conversions (some models use string)
            if (sourceValue is DateOnly sdo && targetValue is DateOnly tdo)
                return sdo == tdo;

            if (sourceValue is DateOnly sdo2 && targetValue is string tstr2)
                return String.Equals(sdo2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), tstr2, StringComparison.Ordinal);

            if (sourceValue is string sstr2 && targetValue is DateOnly tdo2)
                return String.Equals(sstr2, tdo2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal);

            // String normalization
            if (options.TreatNullAndEmptyStringAsEqual && sourceValue is string s1 && targetValue is string s2)
                return String.Equals(s1 ?? string.Empty, s2 ?? string.Empty, StringComparison.Ordinal);

            return Equals(sourceValue, targetValue);
        }

        private static string FormatValue(object v)
        {
            if (v == null) return "<null>";
            if (v is string s) return s;
            return v.ToString() ?? "<null>";
        }

        private static bool TryParseGuid(string value, out Guid guid)
        {
            guid = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Guid.TryParse(value, out guid);
        }

        private static bool HasAnyAttributeByName(PropertyInfo prop, HashSet<string> attributeTypeNames)
        {
            if (attributeTypeNames.Count == 0)
                return false;

            var attrs = prop.GetCustomAttributes(inherit: true);
            foreach (var a in attrs)
            {
                var name = a.GetType().Name;
                if (attributeTypeNames.Contains(name))
                    return true;
            }

            return false;
        }

        private static PropertyMapping ResolveMapping(PropertyInfo sourceProp, ReflectionCompareOptions options)
        {
            // Manual override first (allowed in all modes)
            if (options.NameMap.TryGetValue(sourceProp.Name, out var manualTargetName))
                return PropertyMapping.Simple(manualTargetName);

            // Repo round-trip mode: same-name only (no attribute-driven mapping).
            if (!options.UseMappingAttributes)
                return PropertyMapping.Simple(sourceProp.Name);

            // Attribute-driven by attribute NAME (no hard dependencies)
            // MapToAttribute("X") => target property "X"
            var mapTo = GetAttributeByName(sourceProp, "MapToAttribute");
            if (mapTo != null)
            {
                var toName = ReadFirstStringCtorArg(mapTo);
                if (!string.IsNullOrWhiteSpace(toName))
                    return PropertyMapping.Simple(toName);
            }

            // MapToNameAndIdAttribute("IdField","NameField")
            var mapToNameAndId = GetAttributeByName(sourceProp, "MapToNameAndIdAttribute");
            if (mapToNameAndId != null)
            {
                var args = ReadStringCtorArgs(mapToNameAndId);
                if (args.Count >= 2)
                    return PropertyMapping.EntityHeaderToNameAndId(args[0], args[1]);
            }

            // EncryptedFieldAttribute("EncryptedAmount")
            var encrypted = GetAttributeByName(sourceProp, "EncryptedFieldAttribute");
            if (encrypted != null)
            {
                var encName = ReadFirstStringCtorArg(encrypted);
                if (!string.IsNullOrWhiteSpace(encName))
                    return PropertyMapping.Encrypted(encName);
            }

            // Default: same-name property
            return PropertyMapping.Simple(sourceProp.Name);
        }

        private static object GetAttributeByName(PropertyInfo prop, string attributeTypeName)
        {
            var attrs = prop.GetCustomAttributes(inherit: true);
            foreach (var a in attrs)
            {
                if (String.Equals(a.GetType().Name, attributeTypeName, StringComparison.Ordinal))
                    return a;
            }

            return null;
        }

        private static string ReadFirstStringCtorArg(object attribute)
        {
            var t = attribute.GetType();

            var candidates = new[] { "Name", "Property", "PropertyName", "Target", "TargetProperty", "TargetPropertyName", "Field", "FieldName" };
            foreach (var c in candidates)
            {
                var p = t.GetProperty(c, BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.CanRead && p.PropertyType == typeof(string))
                {
                    var v = (string)p.GetValue(attribute);
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }

            var anyString = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.PropertyType == typeof(string));

            foreach (var p in anyString)
            {
                var v = (string)p.GetValue(attribute);
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }

            return null;
        }

        private static List<string> ReadStringCtorArgs(object attribute)
        {
            var t = attribute.GetType();
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.PropertyType == typeof(string))
                .Select(p => (string)p.GetValue(attribute))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            return props;
        }

        private static void CompareEntityHeaderToNameAndId(
            object source,
            PropertyInfo sourceProp,
            object target,
            Dictionary<string, PropertyInfo> targetProps,
            PropertyMapping mapping,
            ReflectionCompareOptions options,
            ReflectionCompareResult result)
        {
            var sourceType = source.GetType();
            var targetType = target.GetType();

            var sv = sourceProp.GetValue(source);
            if (sv == null)
                return;

            if (!(sv is EntityHeader eh))
            {
                result.Differences.Add($"Expected EntityHeader for {sourceType.Name}.{sourceProp.Name} but was {sv.GetType().Name}");
                return;
            }

            // Id field
            if (!targetProps.TryGetValue(mapping.TargetPropertyName, out var idProp))
            {
                if (options.FailIfTargetPropertyMissing)
                    result.Differences.Add($"Missing target id property: {targetType.Name}.{mapping.TargetPropertyName} (from {sourceType.Name}.{sourceProp.Name})");
            }
            else
            {
                var tv = idProp.GetValue(target);
                if (!AreEqual(eh, tv, typeof(EntityHeader), idProp.PropertyType, options))
                    result.Differences.Add($"Mismatch: {sourceType.Name}.{sourceProp.Name}.Id -> {targetType.Name}.{idProp.Name}. Expected='{FormatValue(eh.Id)}' Actual='{FormatValue(tv)}'");
            }

            // User field
            if (!targetProps.TryGetValue(mapping.TargetPropertyName2, out var nameProp))
            {
                if (options.FailIfTargetPropertyMissing)
                    result.Differences.Add($"Missing target name property: {targetType.Name}.{mapping.TargetPropertyName2} (from {sourceType.Name}.{sourceProp.Name})");
            }
            else
            {
                var tv = nameProp.GetValue(target);
                if (!Equals(eh.Text, tv))
                    result.Differences.Add($"Mismatch: {sourceType.Name}.{sourceProp.Name}.Text -> {targetType.Name}.{nameProp.Name}. Expected='{FormatValue(eh.Text)}' Actual='{FormatValue(tv)}'");
            }
        }

        private enum PropertyMappingKind
        {
            Simple,
            EntityHeaderToNameAndId,
            EncryptedField,
            Skip
        }

        private sealed class PropertyMapping
        {
            public PropertyMappingKind Kind { get; private set; }
            public string TargetPropertyName { get; private set; }
            public string TargetPropertyName2 { get; private set; }

            public static PropertyMapping Simple(string targetPropName)
                => new PropertyMapping { Kind = PropertyMappingKind.Simple, TargetPropertyName = targetPropName };

            public static PropertyMapping EntityHeaderToNameAndId(string idPropName, string namePropName)
                => new PropertyMapping { Kind = PropertyMappingKind.EntityHeaderToNameAndId, TargetPropertyName = idPropName, TargetPropertyName2 = namePropName };

            public static PropertyMapping Encrypted(string encryptedTargetPropName)
                => new PropertyMapping { Kind = PropertyMappingKind.EncryptedField, TargetPropertyName = encryptedTargetPropName };

            public static PropertyMapping Skip()
                => new PropertyMapping { Kind = PropertyMappingKind.Skip, TargetPropertyName = "<skip>" };
        }
    }
}