using LagoVista.Configuration.Client.Models;
using LagoVista.IoT.WebHost.Common.Config;
using Microsoft.Extensions.Configuration;

namespace LagoVista.Relational.MigrationRuntime;

/// <summary>
/// Shared one-shot migration-job configuration bootstrap. No database-specific
/// provider or EF model lives here. The requesting image owns its appKey,
/// target allowlist and expected schema.
/// </summary>
public static class MigrationConfigurationLoader
{
    public const string SourceVariable = "MIGRATION_CONFIG_SOURCE";
    public const string DeploymentVariable = "MIGRATION_DEPLOYMENT";
    public const string TargetSection = "DatabaseMigration:Targets";

    public static async Task<ResolvedMigrationConfiguration> LoadAsync(
        string imageAppKey,
        string target,
        string expectedDatabase,
        string expectedProvider,
        CancellationToken ct = default)
    {
        ValidateIdentity(imageAppKey, target, expectedDatabase, expectedProvider);
        var settings = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

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
            // The deployment key is an operational authorization boundary.
            // Never resolve LIVE implicitly from a developer's machine.
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
                bootstrap, imageAppKey, deployment, ct).ConfigureAwait(false);
            if (!result.Success || result.ResolvedConfiguration?.Values is null)
                throw new InvalidOperationException(
                    $"Config Server resolution failed for registered migration app '{imageAppKey}' and deployment '{deployment}'. Stage={result.FailureStage}; attempts={result.AttemptCount}. No direct connection fallback is permitted.");

            var remoteConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(result.ResolvedConfiguration.Values.Select(pair =>
                    new KeyValuePair<string, string?>(pair.Key, pair.Value)))
                .Build();
            return ResolveRemoteTarget(remoteConfig, target, expectedDatabase,
                expectedProvider, imageAppKey, deployment);
        }

        if (deployment == "live")
            throw new InvalidOperationException("Direct connection mode is disabled for LIVE migration jobs.");

        // Compatibility path for the current DEV-only Kubernetes scripts.
        // It is NEVER used if Config Server bootstrap settings were provided,
        // unless an operator deliberately selects direct mode for local/DEV.
        var connection = settings["MIGRATION_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Direct DEV mode requires MIGRATION_CONNECTION_STRING from a trusted runtime Secret.");

        return new ResolvedMigrationConfiguration(
            "direct", imageAppKey, string.IsNullOrWhiteSpace(deployment) ? "dev" : deployment,
            target, expectedProvider, expectedDatabase, null,
            connection, settings["MIGRATION_BOOTSTRAP_CONNECTION_STRING"]);
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

        var section = configuration.GetSection($"{TargetSection}:{target}");
        var provider = section["Provider"];
        var database = section["Database"];
        var serverHost = section["ServerHost"];
        var connection = section["ConnectionString"];
        if (!string.Equals(provider, expectedProvider, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Config Server provider for target '{target}' does not match the image provider.");
        if (!string.Equals(database, expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException($"Config Server database name for target '{target}' does not match the image's authorized database name.");
        if (string.IsNullOrWhiteSpace(serverHost))
            throw new InvalidOperationException($"Config Server ServerHost for target '{target}' is missing.");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException($"Config Server connection for target '{target}' is missing.");
        return new ResolvedMigrationConfiguration(
            "config-server", imageAppKey, deployment, target, expectedProvider,
            expectedDatabase, serverHost.Trim(), connection,
            section["BootstrapConnectionString"]);
    }

    private static void ValidateIdentity(
        string appKey, string target, string database, string provider)
    {
        if (string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(target) ||
            string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Migration app key, target, database and provider are required.");
    }
}

/// <summary>Secrets must never be serialized into journal output or UI responses.</summary>
public sealed record ResolvedMigrationConfiguration(
    string Source,
    string AppKey,
    string Deployment,
    string Target,
    string Provider,
    string Database,
    string? ServerHost,
    string ConnectionString,
    string? BootstrapConnectionString);
