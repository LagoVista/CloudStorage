namespace SchemaVerify.Core;

public sealed class AppOptions
{
    public string TypeMatchMode { get; set; } = "Family"; // Strict | Family
    public bool FailOnWarnings { get; set; } = false;

    /// <summary>
    /// Optional: explicit assembly paths to load to discover DbContext factories.
    /// If empty, the current AppDomain is used.
    /// </summary>
    public List<string> AssemblyPaths { get; set; } = new();

    public List<ContextSpec> Contexts { get; set; } = new();
    public OutputOptions Output { get; set; } = new();
}

public sealed class OutputOptions
{
    public string? JsonPath { get; set; }
}

public sealed class ContextSpec
{
    /// <summary>
    /// Assembly-qualified name preferred, but "Namespace.Type, Assembly" works too.
    /// </summary>
    public string ContextType { get; set; } = default!;

    public List<TargetSpec> Targets { get; set; } = new();
}

public sealed class TargetSpec
{
    /// <summary>
    /// "sqlserver" | "postgres"
    /// </summary>
    public string Provider { get; set; } = default!;

    public string ConnectionString { get; set; } = default!;
}
