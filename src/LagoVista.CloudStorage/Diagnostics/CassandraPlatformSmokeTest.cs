using Cassandra;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.Diagnostics;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Diagnostics
{
    public sealed class CassandraPlatformSmokeTest : IPlatformSmokeTest
    {
        private readonly ICassandraStorageSettings _settings;
        private readonly ICassandraSessionFactory _sessionFactory;

        public CassandraPlatformSmokeTest(
            ICassandraStorageSettings settings,
            ICassandraSessionFactory sessionFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        }

        public string Key => "storage.cassandra";
        public string Name => "Cassandra";
        public string Category => "Data Storage";

        public async Task<PlatformSmokeTestResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var session = await _sessionFactory.GetSessionAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var rows = await session.ExecuteAsync(
                new SimpleStatement("SELECT release_version FROM system.local")).ConfigureAwait(false);
            var row = rows.FirstOrDefault();
            if (row == null)
                throw new InvalidOperationException("Cassandra system.local query returned no rows.");

            var releaseVersion = row.GetValue<string>("release_version");
            var target = $"{String.Join(",", _settings.ContactPoints)}:{_settings.Port}/{_settings.Keyspace}";

            return new PlatformSmokeTestResult
            {
                Status = PlatformSmokeTestStatus.Passed,
                Target = target,
                Message = String.IsNullOrWhiteSpace(releaseVersion)
                    ? "Cassandra system.local query succeeded."
                    : $"Cassandra system.local query succeeded; release {releaseVersion}."
            };
        }
    }
}
