using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

public static class ExplicitRelationshipAsserts
{
    public static List<string> AssertDtoNavPropertiesAreExplicit(DbContext ctx, Type entityClrType)
    {
        var diffs = new List<string>();

        var model = ctx.GetService<IDesignTimeModel>().Model;
        var et = model.FindEntityType(entityClrType);
        if (et == null)
        {
            diffs.Add($"Entity not in EF model: {entityClrType.FullName}");
            return diffs;
        }

        // Need convention metadata for configuration source
        if (et is not IConventionEntityType cet)
        {
            diffs.Add($"Cannot inspect configuration source for {entityClrType.Name} (not an IConventionEntityType).");
            return diffs;
        }

        foreach (var p in entityClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead) continue;
            if (p.GetIndexParameters().Length != 0) continue;
            if (p.PropertyType == typeof(string)) continue;
            if (IsScalar(p.PropertyType)) continue;

            // Candidate nav property
            var nav = cet.FindNavigation(p.Name);
            if (nav == null)
            {
                // Many-to-many skip nav?
                var skip = cet.FindSkipNavigation(p.Name);
                if (skip != null)
                {
                    // You can enforce explicitness for skip navs too if you use them.
                    var src = skip.GetConfigurationSource();
                    if (src != ConfigurationSource.Explicit)
                        diffs.Add($"Skip navigation not explicit: {entityClrType.Name}.{p.Name} (source={src})");
                    continue;
                }

                diffs.Add($"DTO property looks like navigation but EF does not model it: {entityClrType.Name}.{p.Name}");
                continue;
            }

            // Enforce explicit FK config (strongest)
            var fkSrc = nav.ForeignKey.GetConfigurationSource();
            if (fkSrc != ConfigurationSource.Explicit)
                diffs.Add($"Navigation not explicitly configured: {entityClrType.Name}.{p.Name} (source={fkSrc})");
        }

        return diffs;
    }

    private static bool IsScalar(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;

        if (t.IsPrimitive || t.IsEnum) return true;
        if (t == typeof(Guid) || t == typeof(DateTime) || t.FullName == "System.DateOnly") return true;
        if (t == typeof(TimeSpan) || t == typeof(decimal) || t == typeof(byte[])) return true;

        // Treat dictionaries as scalar-ish for this purpose unless you want to enforce otherwise
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>)) return true;

        return false;
    }
}