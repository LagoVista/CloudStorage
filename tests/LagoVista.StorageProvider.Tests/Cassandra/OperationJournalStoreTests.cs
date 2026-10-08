using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders.Cassandra;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Cassandra
{
    [TestClass]
    [TestCategory("Unit")]
    public class OperationJournalStoreTests
    {
        private static CassandraOperationJournalStore CreateStore() =>
            new CassandraOperationJournalStore(new NoConnectSessionFactory());

        [TestMethod]
        public async Task StartRejectsIncompleteBoundaryIdentityBeforeConnecting()
        {
            var operation = new OperationJournalRecord
            {
                OrganizationId = "org", OperationId = "op", OwnerType = "fix-workspace",
                OwnerId = "fix-1", BoundaryType = ""
            };
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => CreateStore().StartAsync(operation));
        }

        [TestMethod]
        public async Task AppendRejectsInvalidSequenceBeforeConnecting()
        {
            var detail = new OperationJournalDetail
            {
                OrganizationId = "org", OperationId = "op", DetailId = "step", Sequence = 0
            };
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => CreateStore().AppendAsync(detail));
        }

        [TestMethod]
        public async Task TransitionRejectsMissingExpectedStatusBeforeConnecting()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                CreateStore().TransitionAsync("org", "op", "", "failed", "failure", DateTimeOffset.UtcNow));
        }

        [TestMethod]
        public async Task RecoverRejectsMissingExpectedStatusBeforeConnecting()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                CreateStore().RecoverAsync("org", "op", "", "resume", DateTimeOffset.UtcNow));
        }

        [TestMethod]
        public async Task ListRejectsUnknownScopeBeforeConnecting()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                CreateStore().ListAsync(new OperationJournalScope
                {
                    OrganizationId = "org", ScopeType = "unknown"
                }, 20));
        }

        [TestMethod]
        public async Task ListRejectsOversizedPageBeforeConnecting()
        {
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                CreateStore().ListAsync(new OperationJournalScope
                {
                    OrganizationId = "org", ScopeType = "all"
                }, 201));
        }

        [TestMethod]
        public void StandardCassandraStartupRegistersJournalContract()
        {
            var services = new ServiceCollection();
            Startup.ConfigureServices(services);
            Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IOperationJournalStore)
                && d.ImplementationType == typeof(CassandraOperationJournalStore)
                && d.Lifetime == ServiceLifetime.Scoped));
        }

        private sealed class NoConnectSessionFactory : ICassandraSessionFactory
        {
            public Task<Cassandra.ISession> GetSessionAsync() =>
                throw new InvalidOperationException("Validation must run before opening Cassandra.");
        }
    }
}
