using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{

    public sealed class CassandraConnectionSettings 
    {
        public CassandraConnectionSettings(
            IEnumerable<string> contactPoints,
            string userName,
            string password,
            string keyspace,
            int port = 9042,
            string localDataCenter = null)
        {
            if (contactPoints == null) throw new ArgumentNullException(nameof(contactPoints));
            if (String.IsNullOrWhiteSpace(userName)) throw new ArgumentNullException(nameof(userName));
            if (String.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
            if (String.IsNullOrWhiteSpace(keyspace)) throw new ArgumentNullException(nameof(keyspace));
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            var points = new List<string>();
            foreach (var contactPoint in contactPoints)
            {
                if (!String.IsNullOrWhiteSpace(contactPoint))
                {
                    points.Add(contactPoint);
                }
            }

            if (points.Count == 0)
            {
                throw new ArgumentException("At least one Cassandra contact point is required.", nameof(contactPoints));
            }

            ContactPoints = points.AsReadOnly();
            UserName = userName;
            Password = password;
            Keyspace = keyspace;
            Port = port;
            LocalDataCenter = localDataCenter;
        }

        public IReadOnlyList<string> ContactPoints { get; }
        public string UserName { get; }
        public string Password { get; }
        public string Keyspace { get; }
        public int Port { get; }
        public string LocalDataCenter { get; }
    }
}
