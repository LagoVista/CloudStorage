// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 01327be1d3e2ec3c6ab9198c9ab4886279b214c9f61cf064fa61d4fa0e0518ab
// IndexVersion: 2
// --- END CODE INDEX META (do not edit) ---
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using NLog.Layouts;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Utils
{
    public class TestConnections
    {
        public const string ProdSLOrgId = "AA2C78499D0140A5A9CE4B7581EF9691";
        public const string DevSLOrgId = "C8AD4589F26842E7A1AEFBAEFC979C9B";

        public static MongoDocumentStorageConnectionSettings ProductionMongoDocumentStorage => CreateMongoDocumentStorageSettings("PROD");
        public static MongoDocumentStorageConnectionSettings DevMongoDocumentStorage => CreateMongoDocumentStorageSettings("DEV");
        public static MongoDocumentStorageConnectionSettings TestMongoDocumentStorage => CreateLocalTestMongoDocumentStorageSettings();

        public static CassandraStorageSettings ProductionCassandraStorage => CreateCassandraStorageSettings("PROD");
        public static CassandraStorageSettings DevCassandraStorage => CreateCassandraStorageSettings("DEV");

        public static S3ObjectStorageConnectionSettings ProductionS3ObjectStorage => CreateS3ObjectStorageSettings("PROD");
        public static S3ObjectStorageConnectionSettings DevS3ObjectStorage => CreateS3ObjectStorageSettings("DEV");

        public static ConnectionSettings ProductionDocDB
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    Uri = Environment.GetEnvironmentVariable("PROD_DOCDB_ACCOUNT_URL"),
                    AccountId = Environment.GetEnvironmentVariable("PROD_DOCDB_ACCOUNTID"),
                    AccessKey = Environment.GetEnvironmentVariable("PROD_DOCDB_ACCESSKEY"),
                    ResourceName = Environment.GetEnvironmentVariable("PROD_DOCDB_DATABASE"),
                };

                if (String.IsNullOrEmpty(cs.Uri)) Console.WriteLine("[ERROR] - Missing PROD_DOCDB_ACCOUNT_URL as environment variable");
                if (String.IsNullOrEmpty(cs.AccountId)) Console.WriteLine("[ERROR] - Missing PROD_DOCDB_ACCOUNTID as environment variable");
                if (String.IsNullOrEmpty(cs.AccessKey)) Console.WriteLine("[ERROR] - Missing PROD_DOCDB_ACCESSKEY as environment variable");
                if (String.IsNullOrEmpty(cs.ResourceName)) Console.WriteLine("[ERROR] - Missing PROD_DOCDB_DATABASE as environment variable");

                return cs;
            }
        }

        public static ConnectionSettings DefaultDeviceAccountDb
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    Uri = Environment.GetEnvironmentVariable("PROD_DVC_ACCT_URL", EnvironmentVariableTarget.User),
                    Password = Environment.GetEnvironmentVariable("PROD_DVC_ACCT_SYS_PASSWORD", EnvironmentVariableTarget.User),
                    UserName = Environment.GetEnvironmentVariable("PROD_DVC_ACCT_SYS_USER_ID", EnvironmentVariableTarget.User),
                };

                if (String.IsNullOrEmpty(cs.Uri)) Console.WriteLine("[ERROR] - Missing JobScheduler__Database__Url as environment variable");
                if (String.IsNullOrEmpty(cs.UserName)) Console.WriteLine("[ERROR] - Missing JobScheduler__Database__UserName as environment variable");
                if (String.IsNullOrEmpty(cs.Password)) Console.WriteLine("[ERROR] - Missing JobScheduler__Database__Password as environment variable");
                return cs;
            }
        }

        public static ConnectionSettings JobSchedulerDBSettings
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    Uri = Environment.GetEnvironmentVariable("JobScheduler__Database__Url"),
                    UserName = Environment.GetEnvironmentVariable("JobScheduler__Database__UserName"),
                    Password = Environment.GetEnvironmentVariable("JobScheduler__Database__Password"),
                    ResourceName = Environment.GetEnvironmentVariable("JobScheduler__Database__Database"),
                    Port = Environment.GetEnvironmentVariable("JobScheduler__Database__Port"),
                };

                if (String.IsNullOrEmpty(cs.Uri)) Console.WriteLine("[ERROR] - Missing JobScheduler__Database__Url as environment variable");
                if (String.IsNullOrEmpty(cs.UserName)) Console.WriteLine("[ERROR] - Missing JobScheduler__Database__UserName as environment variable");
                if (String.IsNullOrEmpty(cs.Password)) Console.WriteLine("[ERROR] - Missing JobScheduler__Database__Password as environment variable");
                if (String.IsNullOrEmpty(cs.ResourceName)) Console.WriteLine("[ERROR] - Missing JobScheduler__Database__Database as environment variable");
                if (String.IsNullOrEmpty(cs.Port)) Console.WriteLine("[ERROR] - Missing JobScheduler__Database__Port as environment variable");

                return cs;
            }

        }

        public static ConnectionSettings DevDocDB
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    Uri = Environment.GetEnvironmentVariable("DEV_DOCDB_ACCOUNT_URL"),
                    AccountId = Environment.GetEnvironmentVariable("DEV_DOCDB_ACCOUNT_ID"),
                    AccessKey = Environment.GetEnvironmentVariable("DEV_DOCDB_ACCESS_KEY"),
                    ResourceName = Environment.GetEnvironmentVariable("DEV_DOCDB_DATABASE"),
                };

                if (String.IsNullOrEmpty(cs.Uri)) Console.WriteLine("[ERROR] - Missing DEV_DOCDB_ACCOUNT_URL as environment variable");
                if (String.IsNullOrEmpty(cs.AccountId)) Console.WriteLine("[ERROR] - Missing DEV_DOCDB_ACCOUNT_ID as environment variable");
                if (String.IsNullOrEmpty(cs.AccessKey)) Console.WriteLine("[ERROR] - Missing DEV_DOCDB_ACCESS_KEY as environment variable");
                if (String.IsNullOrEmpty(cs.ResourceName)) Console.WriteLine("[ERROR] - Missing DEV_DOCDB_DATABASE as environment variable");

                return cs;
            }
        }

        public static ConnectionSettings ProductionTableStorageDB
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    AccountId = Environment.GetEnvironmentVariable("PROD_TS_STORAGE_ACCOUNT_ID"),
                    AccessKey = Environment.GetEnvironmentVariable("PROD_TS_STORAGE_ACCOUNT_ACCESS_KEY")
                };

                if (String.IsNullOrEmpty(cs.AccountId)) Console.WriteLine("[ERROR] - Missing PROD_TS_STORAGE_ACCOUNT_ID as environment variable");
                if (String.IsNullOrEmpty(cs.AccessKey)) Console.WriteLine("[ERROR] - Missing PROD_TS_STORAGE_ACCOUNT_ACCESS_KEY as environment variable");

                return cs;
            }
        }

        public static ConnectionSettings DevTableStorageDB
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    AccountId = Environment.GetEnvironmentVariable("DEV_TS_STORAGE_ACCOUNT_ID"),
                    AccessKey = Environment.GetEnvironmentVariable("DEV_TS_STORAGE_ACCOUNT_ACCESS_KEY")
                };

                if (String.IsNullOrEmpty(cs.AccountId)) Console.WriteLine("[ERROR] - Missing DEV_TS_STORAGE_ACCOUNT_ID as environment variable");
                if (String.IsNullOrEmpty(cs.AccessKey)) Console.WriteLine("[ERROR] - Missing DEV_TS_STORAGE_ACCOUNT_ACCESS_KEY as environment variable");

                return cs;
            }
        }

        public static ConnectionSettings ProdMetricsStorage
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    Uri = Environment.GetEnvironmentVariable("PROD_METRICS_PSSQL_URL"),
                    UserName = Environment.GetEnvironmentVariable("PROD_METRICS_PSSQL_USER"),
                    ResourceName = Environment.GetEnvironmentVariable("PROD_METRICS_PSSQL_DB"),
                    Password = Environment.GetEnvironmentVariable("PROD_METRICS_PSSQL_PASSWORD")
                };

                if (String.IsNullOrEmpty(cs.Uri)) Console.WriteLine("[ERROR] - Missing PROD_METRICS_PSSQL_URL as environment variable");
                if (String.IsNullOrEmpty(cs.UserName)) Console.WriteLine("[ERROR] - Missing PROD_METRICS_PSSQL_USER as environment variable");
                if (String.IsNullOrEmpty(cs.ResourceName)) Console.WriteLine("[ERROR] - Missing PROD_METRICS_PSSQL_DB as environment variable");
                if (String.IsNullOrEmpty(cs.Password)) Console.WriteLine("[ERROR] - Missing PROD_METRICS_PSSQL_PASSWORD as environment variable");

                return cs;
            }
        }

        public static ConnectionSettings DevMetricsStorage
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    Uri = Environment.GetEnvironmentVariable("DEV_METRICS_PSSQL_URL"),
                    UserName = Environment.GetEnvironmentVariable("DEV_METRICS_PSSQL_USER"),
                    ResourceName = Environment.GetEnvironmentVariable("DEV_METRICS_PSSQL_DB"),
                    Password = Environment.GetEnvironmentVariable("DEV_METRICS_PSSQL_PASSWORD")
                };

                if (String.IsNullOrEmpty(cs.Uri)) Console.WriteLine("[ERROR] - Missing DEV_METRICS_PSSQL_URL as environment variable");
                if (String.IsNullOrEmpty(cs.UserName)) Console.WriteLine("[ERROR] - Missing DEV_METRICS_PSSQL_USER as environment variable");
                if (String.IsNullOrEmpty(cs.ResourceName)) Console.WriteLine("[ERROR] - Missing DEV_METRICS_PSSQL_DB as environment variable");
                if (String.IsNullOrEmpty(cs.Password)) Console.WriteLine("[ERROR] - Missing DEV_METRICS_PSSQL_PASSWORD as environment variable");

                return cs;
            }
        }

        public static ConnectionSettings TestMetricsStorage
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    Uri = Environment.GetEnvironmentVariable("DEV_METRICS_PSSQL_URL"),
                    UserName = Environment.GetEnvironmentVariable("DEV_METRICS_PSSQL_USER"),
                    ResourceName = Environment.GetEnvironmentVariable("TEST_METRICS_PSSQL_DB"),
                    Password = Environment.GetEnvironmentVariable("DEV_METRICS_PSSQL_PASSWORD")
                };

                if (String.IsNullOrEmpty(cs.Uri)) Console.WriteLine("[ERROR] - Missing DEV_METRICS_PSSQL_URL as environment variable");
                if (String.IsNullOrEmpty(cs.UserName)) Console.WriteLine("[ERROR] - Missing DEV_METRICS_PSSQL_USER as environment variable");
                if (String.IsNullOrEmpty(cs.ResourceName)) Console.WriteLine("[ERROR] - Missing TEST_METRICS_PSSQL_DB as environment variable");
                if (String.IsNullOrEmpty(cs.Password)) Console.WriteLine("[ERROR] - Missing DEV_METRICS_PSSQL_PASSWORD as environment variable");

                return cs;
            }
        }

        public static ConnectionSettings ProdSQLServer
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    Uri = Environment.GetEnvironmentVariable("PROD_SQLSRVR_URL"),
                    UserName = Environment.GetEnvironmentVariable("PROD_SQLSRVR_USER"),
                    ResourceName = Environment.GetEnvironmentVariable("PROD_SQLSRVR_DB"),
                    Password = Environment.GetEnvironmentVariable("PROD_SQLSRVR_PASSWORD")
                };

                if (String.IsNullOrEmpty(cs.Uri)) Console.WriteLine("[ERROR] - Missing PROD_SQLSRVR_URL as environment variable");
                if (String.IsNullOrEmpty(cs.UserName)) Console.WriteLine("[ERROR] - Missing PROD_SQLSRVR_USER as environment variable");
                if (String.IsNullOrEmpty(cs.ResourceName)) Console.WriteLine("[ERROR] - Missing PROD_SQLSRVR_DB as environment variable");
                if (String.IsNullOrEmpty(cs.Password)) Console.WriteLine("[ERROR] - Missing PROD_SQLSRVR_PASSWORD as environment variable");

                return cs;
            }
        }

        public static ConnectionSettings DevSQLServer
        {
            get
            {
                var cs = new ConnectionSettings()
                {
                    Uri = Environment.GetEnvironmentVariable("DEV_SQLSRVR_URL"),
                    UserName = Environment.GetEnvironmentVariable("DEV_SQLSRVR_USER"),
                    ResourceName = Environment.GetEnvironmentVariable("DEV_SQLSRVR_DB"),
                    Password = Environment.GetEnvironmentVariable("DEV_SQLSRVR_PASSWORD")
                };

                if (String.IsNullOrEmpty(cs.Uri)) Console.WriteLine("[ERROR] - Missing DEV_SQLSRVR_URL as environment variable");
                if (String.IsNullOrEmpty(cs.UserName)) Console.WriteLine("[ERROR] - Missing DEV_SQLSRVR_USER as environment variable");
                if (String.IsNullOrEmpty(cs.ResourceName)) Console.WriteLine("[ERROR] - Missing DEV_SQLSRVR_DB as environment variable");
                if (String.IsNullOrEmpty(cs.Password)) Console.WriteLine("[ERROR] - Missing DEV_SQLSRVR_PASSWORD as environment variable");

                return cs;
            }
        }

        private static MongoDocumentStorageConnectionSettings CreateMongoDocumentStorageSettings(string prefix)
        {
            var cs = new MongoDocumentStorageConnectionSettings()
            {
              Hosts = new List<string>() { Environment.GetEnvironmentVariable($"{prefix}_MongoDocumentStorage:Hosts")},
              UserName = Environment.GetEnvironmentVariable($"{prefix}_MongoDocumentStorage:UserName"),
              Password = Environment.GetEnvironmentVariable($"{prefix}_MongoDocumentStorage:Password"),
              AuthenticationDatabase = Environment.GetEnvironmentVariable($"{prefix}_MongoDocumentStorage:AuthenticationDatabase"),
              ReplicaSet = Environment.GetEnvironmentVariable($"{prefix}_MongoDocumentStorage:ReplicaSet"),
            };

            if(!String.IsNullOrEmpty(Environment.GetEnvironmentVariable($"{prefix}_MongoDocumentStorage:Port"))) cs.Port = Int32.Parse(Environment.GetEnvironmentVariable($"{prefix}_MongoDocumentStorage:Port"));
            if(!String.IsNullOrEmpty(Environment.GetEnvironmentVariable($"{prefix}_MongoDocumentStorage:UseTls"))) cs.UseTls = Boolean.Parse(Environment.GetEnvironmentVariable($"{prefix}_MongoDocumentStorage:UseTls"));

            if (String.IsNullOrEmpty(cs.UserName)) Console.WriteLine($"[ERROR] - Missing {prefix}_MongoDocumentStorage:UserName as environment variable");
            if (String.IsNullOrEmpty(cs.Password)) Console.WriteLine($"[ERROR] - Missing {prefix}_MongoDocumentStorage:Password as environment variable");
            if (String.IsNullOrEmpty(cs.AuthenticationDatabase)) Console.WriteLine($"[ERROR] - Missing {prefix}_MongoDocumentStorage:AuthenticationDatabase as environment variable");
         
            return cs;
        }

        private static CassandraStorageSettings CreateCassandraStorageSettings(string prefix)
        {
            var values = new Dictionary<string, string>
            {
                ["CassandraStorage:ContactPoints"] = Environment.GetEnvironmentVariable($"{prefix}_CassandraStorage:ContactPoints"),
                ["CassandraStorage:UserName"] = Environment.GetEnvironmentVariable($"{prefix}_CassandraStorage:UserName"),
                ["CassandraStorage:Password"] = Environment.GetEnvironmentVariable($"{prefix}_CassandraStorage:Password"),
                ["CassandraStorage:Keyspace"] = Environment.GetEnvironmentVariable($"{prefix}_CassandraStorage:Keyspace"),
                ["CassandraStorage:Port"] = Environment.GetEnvironmentVariable($"{prefix}_CassandraStorage:Port"),
                ["CassandraStorage:LocalDataCenter"] = Environment.GetEnvironmentVariable($"{prefix}_CassandraStorage:LocalDataCenter")
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            return new CassandraStorageSettings(configuration);
        }

        private static S3ObjectStorageConnectionSettings CreateS3ObjectStorageSettings(string prefix)
        {
            var values = new Dictionary<string, string>
            {
                ["S3ObjectStorage:Host"] = Environment.GetEnvironmentVariable($"{prefix}_S3ObjectStorage:Host"),
                ["S3ObjectStorage:Port"] = Environment.GetEnvironmentVariable($"{prefix}_S3ObjectStorage:Port"),
                ["S3ObjectStorage:AccessKey"] = Environment.GetEnvironmentVariable($"{prefix}_S3ObjectStorage:AccessKey"),
                ["S3ObjectStorage:SecretKey"] = Environment.GetEnvironmentVariable($"{prefix}_S3ObjectStorage:SecretKey"),
                ["S3ObjectStorage:UseTls"] = Environment.GetEnvironmentVariable($"{prefix}_S3ObjectStorage:UseTls"),
                ["S3ObjectStorage:Region"] = Environment.GetEnvironmentVariable($"{prefix}_S3ObjectStorage:Region"),
                ["S3ObjectStorage:PublicHost"] = Environment.GetEnvironmentVariable($"{prefix}_S3ObjectStorage:PublicHost"),
                ["S3ObjectStorage:PublicPort"] = Environment.GetEnvironmentVariable($"{prefix}_S3ObjectStorage:PublicPort"),
                ["S3ObjectStorage:PublicUseTls"] = Environment.GetEnvironmentVariable($"{prefix}_S3ObjectStorage:PublicUseTls")
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            return new S3ObjectStorageConnectionSettings(configuration);
        }

        private static MongoDocumentStorageConnectionSettings CreateLocalTestMongoDocumentStorageSettings()
        {
            var values = new Dictionary<string, string>
            {
                ["MongoDocumentStorage:Hosts"] = "localhost",
                ["MongoDocumentStorage:Port"] = "27018",
                ["MongoDocumentStorage:UserName"] = "nuviot-test",
                ["MongoDocumentStorage:Password"] = "nuviot-test-password",
                ["MongoDocumentStorage:AuthenticationDatabase"] = "admin",
                ["MongoDocumentStorage:UseTls"] = "false"
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            return new MongoDocumentStorageConnectionSettings(configuration);
        }
    }
}