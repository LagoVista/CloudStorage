using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

public static class EfNavigationAsserts
{
    public static List<string> AssertAllReferenceAndCollectionPropertiesAreNavigations(DbContext ctx, Type entityClrType)
    {
        var diffs = new List<string>();

        var model = ctx.GetService<IDesignTimeModel>().Model;
        var et = model.FindEntityType(entityClrType);
        if (et == null)
        {
            diffs.Add($"Entity not in EF model: {entityClrType.FullName}");
            return diffs;
        }

        // All EF-known navigations (including many-to-many skip navigations)
        var efNavs = et.GetNavigations().Select(n => n.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var efSkipNavs = et.GetSkipNavigations().Select(n => n.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // CLR properties that look like navigations
        foreach (var p in entityClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead) continue;

            // Ignore indexers
            if (p.GetIndexParameters().Length != 0) continue;

            // Ignore strings
            if (p.PropertyType == typeof(string)) continue;

            // Ignore primitives/value types and common scalars
            if (IsScalarType(p.PropertyType)) continue;

            // Ignore EF's shadow properties (not CLR) by definition; we're only inspecting CLR properties here.

            var name = p.Name;

            // Is it a collection?
            var isCollection = typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(byte[]);

            // If it's a collection, it should be an EF nav (or skip nav)
            // If it's a reference type, it should be an EF nav
            if (!efNavs.Contains(name) && !efSkipNavs.Contains(name))
            {
                diffs.Add($"Property {entityClrType.Name}.{name} ({Describe(p.PropertyType)}) looks like a navigation " +
                          $"but EF does not model it as a navigation.");
            }
        }

        return diffs;
    }

    private static bool IsScalarType(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;

        if (t.IsPrimitive) return true;
        if (t.IsEnum) return true;

        // Common scalars
        if (t == typeof(Guid)) return true;
        if (t == typeof(DateTime)) return true;
        if (t.FullName == "System.DateOnly") return true;
        if (t == typeof(TimeSpan)) return true;
        if (t == typeof(decimal)) return true;

        // Byte[] is scalar-ish for EF purposes
        if (t == typeof(byte[])) return true;

        return false;
    }

    private static string Describe(Type t)
        => t.IsGenericType ? $"{t.Name}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>" : t.Name;
}