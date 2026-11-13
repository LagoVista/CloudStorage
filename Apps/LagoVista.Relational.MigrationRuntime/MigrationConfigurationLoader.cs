using System.Data.Common;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LagoVista.Configuration.Client.Models;
using LagoVista.IoT.WebHost.Common.Config;
using Microsoft.Extensions.Configuration;

namespace LagoVista.Relational.MigrationRuntime;

/// <summary>
/// Resolve provider-level configuration from the shared db-migrator application.
/// The image, not Config Server, owns the allowlisted target database identity.
/// </summary>
public static class MigrationConfigurationLoader
{
    public const string SourceVariable = "MIGRATION_CONFIG_SOURCE";
    public const string DeploymentVariable = "MIGRATION_DEPLOYMENT";
    public const string SettingsSection = "DbMigrator";

    public static async Task<ResolvedMigrationConfiguration> LoadAsync(
        string imageAppKey,
        string target,
        string expectedDatabase,
        string expectedProvider,
        CancellationToken ct = default)
    {
        ValidateIdentity(imageAppKey, target, expectedDatabase, expectedProvider);
        var settings = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var bootstrapSection = settings.GetSection(ConfigurationBootstrapSettings.SectionName);
        var hasBootstrap = !string.IsNullOrWhiteSpace(bootstrapSection[nameof(ConfigurationBootstrapSettings.ConfigurationServiceBaseUrl)]) ||
                           !string.IsNullOrWhiteSpace(bootstrapSection[nameof(ConfigurationBootstrapSettings.AuthorizationToken)]);
        var mode = (settings[SourceVariable] ?? "auto").Trim().ToLowerInvariant();
        if (mode is not ("auto" or "config-server" or "direct"))
            throw new InvalidOperationException("MIGRATION_CONFIG_SOURCE must be 'auto', 'config-server' or 'direct'.");

        var remote = mode == "config-server" || (mode == "auto" && hasBootstrap);
        var deployment = (settings[DeploymentVariable] ?? "").Trim().ToLowerInvariant();
        if (remote)
        {
            if (deployment is not ("dev" or "live"))
                throw new InvalidOperationException("MIGRATION_DEPLOYMENT must explicitly be 'dev' or 'live' in Config Server mode.");

            var bootstrap = new ConfigurationBootstrapSettings
            {
                ConfigurationServiceBaseUrl = bootstrapSection[nameof(ConfigurationBootstrapSettings.ConfigurationServiceBaseUrl)]!,
                AuthorizationToken = bootstrapSection[nameof(ConfigurationBootstrapSettings.AuthorizationToken)]!,
                TimeoutMs = int.TryParse(bootstrapSection[nameof(ConfigurationBootstrapSettings.TimeoutMs)], out var timeout) &&
                            timeout > 0 ? timeout : 15000
            };
            bootstrap.Validate();
            using var client = new HttpClient();
            var configurationClient = new RemoteConfigurationClient(client);
            var result = await configurationClient.LoadAsync(
                bootstrap, MigrationConfigurationContract.ApplicationKey, deployment, ct).ConfigureAwait(false);
            if (!result.Success || result.ResolvedConfiguration?.Values is null)
                throw new InvalidOperationException(
                    $"Config Server resolution failed for migration app '{MigrationConfigurationContract.ApplicationKey}' and deployment '{deployment}'. Stage={result.FailureStage}; attempts={result.AttemptCount}. No direct connection fallback is permitted.");

            var remoteConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(result.ResolvedConfiguration.Values.Select(pair =>
                    new KeyValuePair<string, string?>(pair.Key, pair.Value)))
                .Build();
            return ResolveRemoteTarget(remoteConfig, target, expectedDatabase,
                expectedProvider, imageAppKey, deployment);
        }

        if (deployment == "live")
            throw new InvalidOperationException("Direct connection mode is disabled for LIVE migration jobs.");

        // Compatibility only for the existing DEV pilot Kubernetes scripts.
        // No Config Server failure ever falls back to these credentials.
        var connection = settings["MIGRATION_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Direct DEV mode requires MIGRATION_CONNECTION_STRING from a trusted runtime Secret.");

        return new ResolvedMigrationConfiguration(
            "direct", MigrationConfigurationContract.ApplicationKey,
            string.IsNullOrWhiteSpace(deployment) ? "dev" : deployment,
            target, expectedProvider, expectedDatabase, null,
            connection);
    }

    public static ResolvedMigrationConfiguration ResolveRemoteTarget(
        IConfiguration configuration,
        string target,
        string expectedDatabase,
        string expectedProvider,
        string imageAppKey,
        string deployment)
    {
        ValidateIdentity(imageAppKey, target, expectedDatabase, expectedProvider);
        if (deployment is not ("dev" or "live"))
            throw new InvalidOperationException("Config Server deployment must be 'dev' or 'live'.");

        var section = configuration.GetSection(SettingsSection);
        var provider = expectedProvider.Trim().ToLowerInvariant();
        string host;
        string connection;
        if (provider == "postgresql")
        {
            var (hostname, port) = ParsePostgresEndpoint(section["PostgresUrl"]);
            host = hostname;
            var username = Required(section["PostgresUserName"], "PostgresUserName");
            var password = Required(section["PostgresPassword"], "PostgresPassword");
            var builder = new DbConnectionStringBuilder
            {
                ["Host"] = host,
                ["Port"] = port,
                ["Database"] = expectedDatabase,
                ["Username"] = username,
                ["Password"] = password,
                ["SSL Mode"] = "Prefer"
            };
            connection = builder.ConnectionString;
        }
        else if (provider == "azure-sql")
        {
            host = Required(section["SqlServerUrl"], "SqlServerUrl");
            if (!Regex.IsMatch(host, @"^(?:tcp:)?[A-Za-z0-9][A-Za-z0-9.-]*(?:[,][0-9]{1,5})?$", RegexOptions.CultureInvariant))
                throw new InvalidOperationException("DbMigrator:SqlServerUrl must be an approved SQL Server host, optionally with a port.");
            var username = Required(section["SqlServerUserName"], "SqlServerUserName");
            var password = Required(section["SqlServerPassword"], "SqlServerPassword");
            var builder = new DbConnectionStringBuilder
            {
                ["Server"] = host,
                ["Initial Catalog"] = expectedDatabase,
                ["User ID"] = username,
                ["Password"] = password,
                ["Encrypt"] = "True",
                ["TrustServerCertificate"] = "False"
            };
            connection = builder.ConnectionString;
        }
        else
        {
            throw new InvalidOperationException("Unknown database migration provider.");
        }

        return new ResolvedMigrationConfiguration(
            "config-server", MigrationConfigurationContract.ApplicationKey,
            deployment, target, provider, expectedDatabase, host,
            connection);
    }

    private static (string Host, int Port) ParsePostgresEndpoint(string? value)
    {
        var endpoint = Required(value, "PostgresUrl");
        var url = endpoint.Contains("://", StringComparison.Ordinal) ? endpoint : "postgresql://" + endpoint;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("postgresql" or "postgres") ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            uri.AbsolutePath is not ("" or "/"))
            throw new InvalidOperationException("DbMigrator:PostgresUrl must be a PostgreSQL host or endpoint URL without credentials, database path or query.");
        var port = uri.IsDefaultPort || uri.Port == -1 ? 5432 : uri.Port;
        if (port is < 1 or > 65535)
            throw new InvalidOperationException("DbMigrator:PostgresUrl has an invalid port.");
        return (uri.Host, port);
    }

    private static string Required(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"DbMigrator:{name} is required for this provider.");
        return value.Trim();
    }

    private static void ValidateIdentity(string appKey, string target, string database, string provider)
    {
        if (!string.Equals(appKey, MigrationConfigurationContract.ApplicationKey, StringComparison.Ordinal))
            throw new InvalidOperationException("Database migration images must use the shared db-migrator Config Server application.");
        if (string.IsNullOrWhiteSpace(target) ||
            !Regex.IsMatch(target, "^[a-z][a-z0-9-]*$", RegexOptions.CultureInvariant) ||
            string.IsNullOrWhiteSpace(database) ||
            !Regex.IsMatch(database, "^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant) ||
            provider is not ("postgresql" or "azure-sql"))
            throw new ArgumentException("An approved migration target, database and supported provider are required.");
    }
}

/// <summary>Never serialize connection strings into UI or journal records.</summary>
public sealed record ResolvedMigrationConfiguration(
    string Source,
    string AppKey,
    string Deployment,
    string Target,
    string Provider,
    string Database,
    string? ServerHost,
    [property: JsonIgnore] string ConnectionString)
{
    public override string ToString() =>
        $"Migration target {AppKey}/{Deployment}/{Target}; provider={Provider}; database={Database}; host={ServerHost ?? "unregistered"}; source={Source}; credentials=[redacted]";
}
