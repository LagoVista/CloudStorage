using LagoVista.CloudStorage.Storage.Connections;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.Diagnostics;
using Npgsql;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.Relational.Diagnostics
{
    public sealed class PostgresPlatformSmokeTest : IPlatformSmokeTest
    {
        private readonly IPostgresConnectionSettings _settings;

        public PostgresPlatformSmokeTest(IPostgresConnectionSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public string Key => "postgres";
        public string Name => "PostgreSQL";
        public string Category => "Storage";

        public async Task<PlatformSmokeTestResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var target = $"{_settings.HostName}:{_settings.Port}";

            try
            {
                var connectionString = new NpgsqlConnectionStringBuilder
                {
                    Host = _settings.HostName,
                    Port = _settings.Port,
                    Username = _settings.UserName,
                    Password = _settings.Password,
                    Timeout = 5,
                    CommandTimeout = 5,
                    Pooling = false
                }.ConnectionString;

                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                await using (var command = new NpgsqlCommand("SELECT 1", connection))
                {
                    var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    if (Convert.ToInt32(value) != 1)
                        throw new InvalidOperationException("PostgreSQL connectivity probe returned an unexpected result.");
                }

                if (!String.IsNullOrWhiteSpace(_settings.SchemaName))
                {
                    await using var schemaCommand = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema)", connection);
                    schemaCommand.Parameters.AddWithValue("schema", _settings.SchemaName);
                    var schemaExists = Convert.ToBoolean(await schemaCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
                    if (!schemaExists)
                        throw new InvalidOperationException($"Configured PostgreSQL schema '{_settings.SchemaName}' was not found in database '{connection.Database}'.");
                }

                stopwatch.Stop();
                return new PlatformSmokeTestResult
                {
                    Key = Key,
                    Name = Name,
                    Category = Category,
                    Status = PlatformSmokeTestStatus.Passed,
                    Target = target,
                    Message = $"Connected to PostgreSQL database '{connection.Database}' and validated schema '{_settings.SchemaName}'.",
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    CheckedUtc = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new PlatformSmokeTestResult
                {
                    Key = Key,
                    Name = Name,
                    Category = Category,
                    Status = PlatformSmokeTestStatus.Failed,
                    Target = target,
                    Message = ex.Message,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    CheckedUtc = DateTime.UtcNow
                };
            }
        }
    }
}
