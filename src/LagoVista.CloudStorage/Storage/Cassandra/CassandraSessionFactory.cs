using Cassandra;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public interface ICassandraSessionFactory
    {
        Task<ISession> GetSessionAsync();
    }

    /// <summary>
    /// Owns the shared Cassandra driver cluster/session for the configured application keyspace.
    /// Keyspace creation is intentionally a platform/bootstrap responsibility.
    /// </summary>
    public sealed class CassandraSessionFactory : ICassandraSessionFactory, IDisposable
    {
        private readonly ICassandraStorageSettings _settings;
        private readonly object _sync = new object();
        private ICluster _cluster;
        private Task<ISession> _sessionTask;

        public CassandraSessionFactory(ICassandraStorageSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Task<ISession> GetSessionAsync()
        {
            lock (_sync)
            {
                if (_sessionTask != null) return _sessionTask;

                var builder = Cluster.Builder()
                    .AddContactPoints(_settings.ContactPoints)
                    .WithPort(_settings.Port)
                    .WithCredentials(_settings.UserName, _settings.Password);

                if (!String.IsNullOrWhiteSpace(_settings.LocalDataCenter))
                {
                    builder = builder.WithLoadBalancingPolicy(
                        new DCAwareRoundRobinPolicy(_settings.LocalDataCenter));
                }

                _cluster = builder.Build();
                _sessionTask = _cluster.ConnectAsync(_settings.Keyspace);
                return _sessionTask;
            }
        }

        public void Dispose()
        {
            if (_sessionTask != null && _sessionTask.Status == TaskStatus.RanToCompletion)
            {
                _sessionTask.Result?.Dispose();
            }

            _cluster?.Dispose();
        }
    }
}
