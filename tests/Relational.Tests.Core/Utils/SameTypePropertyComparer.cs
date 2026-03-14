using LagoVista.Core.Models;
using LagoVista.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Relational.Tests.Core.Utils
{
    public static class SameTypePropertyComparer
    {
        public sealed class Result
        {
            public List<string> Differences { get; } = new();
            public bool Success => Differences.Count == 0;

            public override string ToString()
                => Success ? "No differences." : string.Join(Environment.NewLine, Differences);
        }

        public static Result Compare<T>(T expected, T actual, params string[] ignorePropertyNames)
            where T : class
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            if (ReferenceEquals(expected, actual))
                throw new ArgumentException(
                    "Expected and actual are the same reference. This often happens when EF returns the same tracked instance. " +
                    "Use AsNoTracking(), a new DbContext, or detach/clear the change tracker before reloading.");

            var et = expected.GetType();
            var at = actual.GetType();

            if (!ReferenceEquals(et, at))
                throw new InvalidOperationException($"SameTypePropertyComparer requires identical runtime types. Expected={et.FullName}, Actual={at.FullName}");

            var ignore = new HashSet<string>(ignorePropertyNames ?? Array.Empty<string>(), StringComparer.Ordinal);

            var props = et
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Where(p => p.GetIndexParameters().Length == 0);

            var result = new Result();

            foreach (var p in props)
            {
                
                if (ignore.Contains(p.Name))
                    continue;

                var type = p.PropertyType;

                if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
                    continue;

                var ev = p.GetValue(expected);
                var av = p.GetValue(actual);

                if (AreEqual(ev, av))
                    continue;

                result.Differences.Add(
                    $"Mismatch: {et.Name}.{p.Name}. Expected='{Format(ev)}' Actual='{Format(av)}'");
            }

            return result;
        }

        private static bool AreEqual(object expected, object actual)
        {
            if (expected == null && actual == null)
                return true;

            if (expected == null || actual == null)
                return false;

            // EntityHeader (and EntityHeader<T>) compare: Id + Text + Key
            if (expected is EntityHeader eh1 && actual is EntityHeader eh2)
            {
                return String.Equals(eh1.Id, eh2.Id, StringComparison.Ordinal)
                    && String.Equals(eh1.Text, eh2.Text, StringComparison.Ordinal)
                    && String.Equals(eh1.Key, eh2.Key, StringComparison.Ordinal);
            }

            if (expected is AppUserDTO au1 && actual is AppUserDTO au2)
            {
                return String.Equals(au1.AppUserId, au2.AppUserId, StringComparison.Ordinal)
                    && String.Equals(au1.FullName, au2.FullName, StringComparison.Ordinal);
            }

            // Guid <-> string (optional convenience)
            if (expected is Guid g1 && actual is string s2)
                return String.Equals(g1.ToString(), s2, StringComparison.OrdinalIgnoreCase);

            if (expected is string s1 && actual is Guid g2)
                return Guid.TryParse(s1, out var pg) && pg == g2;

            // DateOnly <-> string yyyy-MM-dd (optional convenience)
            if (expected is DateOnly d1 && actual is string ds2)
                return String.Equals(d1.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ds2, StringComparison.Ordinal);

            if (expected is string ds1 && actual is DateOnly d2)
                return String.Equals(ds1, d2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal);

            return Equals(expected, actual);
        }

        private static string Format(object v)
        {
            if (v == null) return "<null>";
            if (v is string s) return s;

            if (v is EntityHeader eh)
                return $"EntityHeader(Id='{eh.Id}', Text='{eh.Text}', Key='{eh.Key}')";

            return v.ToString() ?? "<null>";
        }
    }
}