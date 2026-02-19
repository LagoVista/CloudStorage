using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchemaVerify.Core;

public static class DbContextFactoryResolver
{
    public static DbContext CreateDbContext(string contextTypeName, string provider, string connectionString, IEnumerable<Assembly> assemblies)
    {
        var contextType = ResolveType(contextTypeName, assemblies)
            ?? Type.GetType(contextTypeName, throwOnError: false);

        if (contextType is null)
        {
            throw new InvalidOperationException($"Could not resolve DbContext type '{contextTypeName}'. Ensure the assembly is loaded and the type name is correct.");
        }

        // Preferred: find IDesignTimeDbContextFactory<TContext>
        var factory = FindDesignTimeFactory(contextType, assemblies);
        if (factory is not null)
        {
            // We pass simple args: provider + connection string so your factories can route appropriately.
            // args[0] = provider, args[1] = connectionString
            var created = (DbContext?)factory.CreateDbContext(new[] { provider, connectionString });
            if (created is null)
            {
                throw new InvalidOperationException($"Design-time factory for '{contextType.FullName}' returned null.");
            }

            return created;
        }

        throw new InvalidOperationException(
            $"No IDesignTimeDbContextFactory<{contextType.Name}> found for '{contextType.FullName}'. " +
            "Add a design-time factory in your EF project so SchemaVerify can instantiate the context with a connection string.");
    }

    private static Type? ResolveType(string typeName, IEnumerable<Assembly> assemblies)
    {
        // Try direct match by FullName
        foreach (var a in assemblies)
        {
            var t = a.GetTypes().FirstOrDefault(x => string.Equals(x.FullName, typeName, StringComparison.Ordinal));
            if (t is not null) return t;
        }

        // Try "Namespace.Type, Assembly" format
        var parts = typeName.Split(',', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            var simpleName = parts[0];
            foreach (var a in assemblies)
            {
                var t = a.GetTypes().FirstOrDefault(x => string.Equals(x.FullName, simpleName, StringComparison.Ordinal));
                if (t is not null) return t;
            }
        }

        return null;
    }

    private static dynamic? FindDesignTimeFactory(Type contextType, IEnumerable<Assembly> assemblies)
    {
        var factoryInterface = typeof(IDesignTimeDbContextFactory<>).MakeGenericType(contextType);

        foreach (var a in assemblies)
        {
            foreach (var t in a.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface) continue;
                if (!factoryInterface.IsAssignableFrom(t)) continue;

                var instance = Activator.CreateInstance(t);
                if (instance is null) continue;
                return instance;
            }
        }

        return null;
    }
}
