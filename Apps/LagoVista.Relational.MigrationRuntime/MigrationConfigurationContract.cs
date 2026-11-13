namespace LagoVista.Relational.MigrationRuntime;

/// <summary>
/// One shared Config Server application for all approved database migration images.
/// A declarative manifest contains setting names only; passwords are classified
/// as secrets in Config Server and never embedded in the Docker image.
/// </summary>
public static class MigrationConfigurationContract
{
    public const string ApplicationKey = "db-migrator";

    public static string ExportConfigManifest()
    {
        var names = new[]
        {
            "DbMigrator:SqlServerUrl",
            "DbMigrator:SqlServerUserName",
            "DbMigrator:SqlServerPassword",
            "DbMigrator:PostgresUrl",
            "DbMigrator:PostgresUserName",
            "DbMigrator:PostgresPassword"
        };

        var lines = new List<string> { "BEGIN CONFIG" };
        foreach (var name in names)
            lines.Add(name + ",false,false");
        lines.Add("END CONFIG");
        return string.Join(Environment.NewLine, lines);
    }
}
