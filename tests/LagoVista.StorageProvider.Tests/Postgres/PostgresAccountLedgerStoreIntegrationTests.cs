using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Relational.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Postgres
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Postgres")]
    [TestCategory("PostgresAccountLedger")]
    public class PostgresAccountLedgerStoreIntegrationTests
    {
        [TestMethod]
        public async Task AddTransactionAsync_MaintainsBalanceAndIntegrityChain()
        {
            var settings = new TestAccountLedgerStorageSettings();
            using var services = CreateServices(settings);
            var store = services.GetRequiredService<IAccountLedgerStore<TestLedgerRecord>>();
            var organizationId = NewId();
            var accountId = NewId();
            var start = new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

            var credit = await store.AddTransactionAsync(CreateCredit(organizationId, accountId, "WORK_UNITS", 100m, start));
            var debit = await store.AddTransactionAsync(CreateDebit(organizationId, accountId, "WORK_UNITS", 40m, start.AddMinutes(1)));

            Assert.AreEqual(100m, credit.Balance);
            Assert.AreEqual(60m, debit.Balance);
            Assert.IsFalse(String.IsNullOrWhiteSpace(credit.IntegrityHash));
            Assert.IsFalse(String.IsNullOrWhiteSpace(debit.IntegrityHash));
            Assert.AreNotEqual(credit.IntegrityHash, debit.IntegrityHash);
            Assert.AreEqual(60m, await store.GetBalanceAsync(organizationId, accountId, "WORK_UNITS"));

            await using var connection = await OpenConnectionAsync(settings);
            await using var command = new NpgsqlCommand("SELECT previous_integrity_hash, integrity_hash FROM \"AccountLedger\".account_ledger_entries WHERE organization_id = @org AND account_id = @account AND transaction_type = @type ORDER BY sequence", connection);
            command.Parameters.AddWithValue("org", organizationId);
            command.Parameters.AddWithValue("account", accountId);
            command.Parameters.AddWithValue("type", "WORK_UNITS");
            await using var reader = await command.ExecuteReaderAsync();
            Assert.IsTrue(await reader.ReadAsync());
            Assert.AreEqual(String.Empty, reader.GetString(0));
            var firstHash = reader.GetString(1);
            Assert.IsTrue(await reader.ReadAsync());
            Assert.AreEqual(firstHash, reader.GetString(0));
        }

        [TestMethod]
        public async Task AddTransactionAsync_SerializesConcurrentWritesToSameLedger()
        {
            var settings = new TestAccountLedgerStorageSettings();
            using var services = CreateServices(settings);
            var store = services.GetRequiredService<IAccountLedgerStore<TestLedgerRecord>>();
            var organizationId = NewId();
            var accountId = NewId();
            var created = DateTime.UtcNow;

            var writes = Enumerable.Range(0, 20).Select(index => store.AddTransactionAsync(CreateCredit(organizationId, accountId, "CONCURRENT", 1m, created.AddTicks(index)))).ToArray();
            await Task.WhenAll(writes);

            Assert.AreEqual(20m, await store.GetBalanceAsync(organizationId, accountId, "CONCURRENT"));

            var result = await store.QueryAsync(new AccountLedgerQuery { OrganizationId = organizationId, AccountId = accountId, TransactionType = "CONCURRENT", Page = new StoragePageRequest(100) });
            Assert.AreEqual(20, result.Items.Count);
            CollectionAssert.AreEqual(Enumerable.Range(1, 20).Reverse().Select(value => (decimal)value).ToArray(), result.Items.Select(item => item.Balance).ToArray());
            Assert.AreEqual(20, result.Items.Select(item => item.IntegrityHash).Distinct().Count());
        }

        [TestMethod]
        public async Task QueryAsync_SupportsDateRangeAndStablePaging()
        {
            var settings = new TestAccountLedgerStorageSettings();
            using var services = CreateServices(settings);
            var store = services.GetRequiredService<IAccountLedgerStore<TestLedgerRecord>>();
            var organizationId = NewId();
            var accountId = NewId();
            var start = new DateTime(2026, 8, 26, 13, 0, 0, DateTimeKind.Utc);
            var records = Enumerable.Range(1, 5).Select(index => CreateCredit(organizationId, accountId, "PAGING", index, start.AddMinutes(index))).ToArray();

            foreach (var record in records) await store.AddTransactionAsync(record);

            var firstPage = await store.QueryAsync(new AccountLedgerQuery { OrganizationId = organizationId, AccountId = accountId, TransactionType = "PAGING", Page = new StoragePageRequest(2) });
            Assert.AreEqual(2, firstPage.Items.Count);
            CollectionAssert.AreEqual(new[] { records[4].Id, records[3].Id }, firstPage.Items.Select(item => item.Record.Id).ToArray());
            Assert.IsTrue(firstPage.HasMoreRecords);

            var secondPage = await store.QueryAsync(new AccountLedgerQuery { OrganizationId = organizationId, AccountId = accountId, TransactionType = "PAGING", Page = new StoragePageRequest(2, firstPage.ContinuationToken) });
            CollectionAssert.AreEqual(new[] { records[2].Id, records[1].Id }, secondPage.Items.Select(item => item.Record.Id).ToArray());
            Assert.IsTrue(secondPage.HasMoreRecords);

            var thirdPage = await store.QueryAsync(new AccountLedgerQuery { OrganizationId = organizationId, AccountId = accountId, TransactionType = "PAGING", Page = new StoragePageRequest(2, secondPage.ContinuationToken) });
            Assert.AreEqual(records[0].Id, thirdPage.Items.Single().Record.Id);
            Assert.IsFalse(thirdPage.HasMoreRecords);

            var range = await store.QueryAsync(new AccountLedgerQuery { OrganizationId = organizationId, AccountId = accountId, TransactionType = "PAGING", StartDate = start.AddMinutes(2), EndDate = start.AddMinutes(4), Page = new StoragePageRequest(10) });
            CollectionAssert.AreEqual(new[] { records[3].Id, records[2].Id, records[1].Id }, range.Items.Select(item => item.Record.Id).ToArray());
        }

        [TestMethod]
        public async Task InvalidOrDuplicateTransactions_DoNotCorruptBalance()
        {
            var settings = new TestAccountLedgerStorageSettings();
            using var services = CreateServices(settings);
            var store = services.GetRequiredService<IAccountLedgerStore<TestLedgerRecord>>();
            var organizationId = NewId();
            var accountId = NewId();
            var valid = CreateCredit(organizationId, accountId, "VALIDATION", 25m, DateTime.UtcNow);

            await store.AddTransactionAsync(valid);

            await AssertFailsAsync(() => store.AddTransactionAsync(new TestLedgerRecord { Id = NewId(), OrganizationId = organizationId, Organization = "Contract Organization", AccountId = accountId, Account = "Contract Account", TransactionType = "VALIDATION", CreditAmount = 1m, DebitAmount = 1m, CreationDate = DateTime.UtcNow }));
            await AssertFailsAsync(() => store.AddTransactionAsync(new TestLedgerRecord { Id = valid.Id, OrganizationId = organizationId, Organization = "Contract Organization", AccountId = accountId, Account = "Contract Account", TransactionType = "VALIDATION", CreditAmount = 10m, CreationDate = DateTime.UtcNow }));

            Assert.AreEqual(25m, await store.GetBalanceAsync(organizationId, accountId, "VALIDATION"));
        }

        [TestMethod]
        public void DependencyRegistration_ResolvesConcretePostgresStore()
        {
            var settings = new TestAccountLedgerStorageSettings();
            using var services = CreateServices(settings);
            var store = services.GetRequiredService<IAccountLedgerStore<TestLedgerRecord>>();
            Assert.IsInstanceOfType<PostgresAccountLedgerStore<TestLedgerRecord>>(store);
        }

        private static ServiceProvider CreateServices(IAccountLedgerStorageSettings settings)
        {
            var services = new ServiceCollection();
            services.AddSingleton(settings);
            services.AddSingleton<IAccountLedgerStorageSettings>(settings);
            services.AddPostgresAccountLedgerStore<TestLedgerRecord>();
            return services.BuildServiceProvider();
        }

        private static async Task<NpgsqlConnection> OpenConnectionAsync(IAccountLedgerStorageSettings settings)
        {
            var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder { Host = settings.HostName, Port = settings.Port, Username = settings.UserName, Password = settings.Password, Pooling = false }.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static TestLedgerRecord CreateCredit(string organizationId, string accountId, string transactionType, decimal amount, DateTime creationDate) => new TestLedgerRecord { Id = NewId(), OrganizationId = organizationId, Organization = "Contract Organization", AccountId = accountId, Account = "Contract Account", TransactionType = transactionType, CreditAmount = amount, CreationDate = creationDate };
        private static TestLedgerRecord CreateDebit(string organizationId, string accountId, string transactionType, decimal amount, DateTime creationDate) => new TestLedgerRecord { Id = NewId(), OrganizationId = organizationId, Organization = "Contract Organization", AccountId = accountId, Account = "Contract Account", TransactionType = transactionType, DebitAmount = amount, CreationDate = creationDate };
        private static string NewId() => Guid.NewGuid().ToString("N").ToUpperInvariant();

        private static async Task AssertFailsAsync(Func<Task> action)
        {
            try
            {
                await action();
                Assert.Fail("Expected operation to fail.");
            }
            catch (AssertFailedException)
            {
                throw;
            }
            catch
            {
            }
        }

        private sealed class TestAccountLedgerStorageSettings : IAccountLedgerStorageSettings
        {
            public string HostName => "127.0.0.1";
            public string UserName => "postgres";
            public string Password => String.Empty;
            public int Port => 19044;
            public string SchemaName => "AccountLedger";
        }

        public sealed class TestLedgerRecord : IAccountLedgerRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public string AccountId { get; set; }
            public string Account { get; set; }
            public string TransactionType { get; set; }
            public decimal? CreditAmount { get; set; }
            public decimal? DebitAmount { get; set; }
            public DateTime CreationDate { get; set; }
        }
    }
}
