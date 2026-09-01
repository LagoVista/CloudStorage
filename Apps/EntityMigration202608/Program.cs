using EntityMigration202608;
using LagoVista;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

const string defaultAppKey = "web";
const string defaultDeploymentKey = "dev";
const string defaultConfigurationServiceBaseUrl = "https://config.nuviot.com";

var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "status";
var requestedDeployment = args.Length > 1
    ? args[1].Trim()
    : ReadOptionalEnvironmentVariable("CFG_ENVIRONMENT_KEY") ?? defaultDeploymentKey;
var deploymentKey = NormalizeDeploymentKey(requestedDeployment);
var migrationEnvironment = deploymentKey;
var appKey = ReadOptionalEnvironmentVariable("CFG_APP_KEY") ?? defaultAppKey;
var baseUrl = ReadOptionalEnvironmentVariable("CFG_SRVR_URL") ?? defaultConfigurationServiceBaseUrl;
var tokenEnvironmentVariable = BuildTokenEnvironmentVariableName(appKey, deploymentKey);
var token = ReadOptionalEnvironmentVariable(tokenEnvironmentVariable);

if (String.IsNullOrWhiteSpace(token))
    throw new InvalidOperationException($"Missing remote configuration token environment variable '{tokenEnvironmentVariable}'.");

var maxPages = args.Length > 2 && Int32.TryParse(args[2], out var parsedMaxPages) ? parsedMaxPages : 5;
var batchSize = args.Length > 3 && Int32.TryParse(args[3], out var parsedBatchSize) ? parsedBatchSize : 200;

Console.WriteLine("Entity migration configuration bootstrap");
Console.WriteLine($"Application:          {appKey}");
Console.WriteLine($"Deployment:           {deploymentKey}");
Console.WriteLine($"Configuration server: {baseUrl}");
Console.WriteLine($"Token variable:       {tokenEnvironmentVariable}");
Console.WriteLine();

IConfigurationRoot configuration;
var bootstrapServices = new ServiceCollection();
bootstrapServices.AddRemoteConfigurationClient();
using (var bootstrapProvider = bootstrapServices.BuildServiceProvider())
{
    var remoteConfigurationClient = bootstrapProvider.GetRequiredService<IRemoteConfigurationClient>();
    configuration = await remoteConfigurationClient.LoadAsync(
        new RemoteConfigurationSettings
        {
            ConfigurationServiceBaseUrl = baseUrl,
            AuthorizationToken = token
        },
        appKey,
        deploymentKey);
}

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<IConfigurationRoot>(configuration);
LagoVista.CloudStorage.Startup.ConfigureServices(services);

using var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();

var runner = new EntityMigrationRunner(
    migrationEnvironment,
    scope.ServiceProvider.GetRequiredService<ICosmosConnectionSettings>(),
    scope.ServiceProvider.GetRequiredService<IMongoDocumentStorageConnectionSettings>(),
    scope.ServiceProvider.GetRequiredService<IApplicationDataStorageSettings>(),
    scope.ServiceProvider.GetRequiredService<IDocumentMigrationService>());

var shutdownCts = new CancellationTokenSource();

void RequestShutdown()
{
    try
    {
        if (!shutdownCts.IsCancellationRequested)
            shutdownCts.Cancel();
    }
    catch (ObjectDisposedException)
    {
        // Process shutdown may race with normal cleanup.
    }
}

ConsoleCancelEventHandler cancelHandler = (_, e) =>
{
    Console.WriteLine("Ctrl+C received, shutting down...");
    e.Cancel = true;
    RequestShutdown();
};

EventHandler processExitHandler = (_, _) => RequestShutdown();

Console.CancelKeyPress += cancelHandler;
AppDomain.CurrentDomain.ProcessExit += processExitHandler;

try
{
    switch (mode)
    {
        case "status":
            await runner.ShowStatusAsync(shutdownCts.Token);
            break;

        case "dry-run":
        case "migrate-entities-dryrun":
            await runner.DryRunAsync(batchSize, maxPages, shutdownCts.Token);
            break;

        case "migrate":
        case "migrate-entities":
            await runner.MigrateAsync(batchSize, maxPages, shutdownCts.Token);
            break;

        case "validate":
        case "validate-entities":
            await runner.ValidateAsync(batchSize, shutdownCts.Token);
            break;

        case "reset":
        case "reset-mongo-entities":
            await runner.ResetMongoAsync(shutdownCts.Token);
            break;

        default:
            Console.WriteLine($"Unknown mode '{mode}'.");
            Console.WriteLine("Usage: EntityMigration202608 <status|dry-run|migrate|validate|reset> [deployment] [maxPages] [batchSize]");
            Environment.ExitCode = 1;
            break;
    }
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
    AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
    shutdownCts.Dispose();
}

static string NormalizeDeploymentKey(string value)
{
    if (String.IsNullOrWhiteSpace(value)) return "dev";
    return value.Trim().Equals("prod", StringComparison.OrdinalIgnoreCase) ? "live" : value.Trim().ToLowerInvariant();
}

static string BuildTokenEnvironmentVariableName(string appKey, string deploymentKey)
{
    return $"CFG_{ToEnvironmentVariableSegment(appKey)}_{ToEnvironmentVariableSegment(deploymentKey)}_TOKEN";
}

static string ToEnvironmentVariableSegment(string value)
{
    var result = new StringBuilder(value.Length);
    foreach (var character in value)
        result.Append(Char.IsLetterOrDigit(character) ? Char.ToUpperInvariant(character) : '_');
    return result.ToString();
}

static string? ReadOptionalEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (String.IsNullOrWhiteSpace(value) && OperatingSystem.IsWindows())
        value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
    return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
