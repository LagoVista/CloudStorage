namespace LagoVista.Relational.MigrationRuntime;

/// <summary>
/// Declarative Config Server registration requirements for a one-shot database
/// migration application. This manifest has NO credential values, endpoints or
/// physical database dependencies. Secret classification is an administrator
/// registration property, not embedded in the legacy manifest protocol.
/// </summary>
public static class MigrationConfigurationContract
{
    private static readonly string[] RequiredNames =
    [
        "Provider", "Database", "ServerHost", "ConnectionString"
    ];

    public static string ExportConfigManifest(IEnumerable<string> allowedTargets)
    {
        ArgumentNullException.ThrowIfNull(allowedTargets);

        // Protect Config Server setting keys from malformed or arbitrary paths.
        var targets = allowedTargets.ToArray();
        if (targets.Length == 0 || targets.Distinct(StringComparer.Ordinal).Count() != targets.Length ||
            targets.Any(target => string.IsNullOrWhiteSpace(target) ||
                                  !target.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
            throw new InvalidOperationException("Migration targets must be unique, nonempty lowercase canonical keys.");

        var lines = new List<string> { "BEGIN CONFIG" };
        foreach (var target in targets)
        {
            var prefix = $"{MigrationConfigurationLoader.TargetSection}:{target}";
            foreach (var name in RequiredNames)
                lines.Add($"{prefix}:{name},false,false");
            lines.Add($"{prefix}:BootstrapConnectionString,true,false");
        }

        lines.Add("END CONFIG");
        return string.Join(Environment.NewLine, lines);
    }
}
