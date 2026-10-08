using LagoVista.CloudStorage.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Cassandra
{
    [TestClass]
    [TestCategory("Unit")]
    public class OperationJournalAuthorizationTests
    {
        [TestMethod]
        public async Task UntrustedCallerCannotStartJournalEntry()
        {
            var stub = new RecordingStore();
            var service = new OperationJournalAccessService(stub);
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
                service.StartAsync(new OperationJournalPrincipal { OrganizationId = "org" },
                    new OperationJournalRecord { OrganizationId = "org" }));
            Assert.AreEqual(0, stub.Calls);
        }

        [TestMethod]
        public async Task InternalExecutorCannotWriteAnotherTenant()
        {
            var stub = new RecordingStore();
            var service = new OperationJournalAccessService(stub);
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
                service.AppendAsync(new OperationJournalPrincipal
                {
                    OrganizationId = "org-a", InternalDeterministicExecutor = true
                }, new OperationJournalDetail { OrganizationId = "org-b" }));
            Assert.AreEqual(0, stub.Calls);
        }

        [TestMethod]
        public async Task OwnerHistoryRejectsUnauthorizedWorkstream()
        {
            var stub = new RecordingStore();
            var service = new OperationJournalAccessService(stub);
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
                service.ListAsync(new OperationJournalPrincipal
                {
                    OrganizationId = "org", WorkstreamIds = new[] { "allowed" }
                }, new OperationJournalScope
                {
                    OrganizationId = "org", ScopeType = "workstream", WorkstreamId = "denied"
                }, 20));
            Assert.AreEqual(0, stub.Calls);
        }

        [TestMethod]
        public async Task UnauthorizedCallerCannotReadOperationDetails()
        {
            var stub = new RecordingStore();
            var service = new OperationJournalAccessService(stub);
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
                service.GetDetailsAsync(new OperationJournalPrincipal
                {
                    OrganizationId = "org", WorkstreamIds = new[] { "different" }
                }, "operation-1", 20));
            Assert.AreEqual(0, stub.DetailsCalls);
        }

        [TestMethod]
        public async Task AuthorizedWorkstreamCanReadChildWorkspaceOperation()
        {
            var stub = new RecordingStore();
            var service = new OperationJournalAccessService(stub);
            var item = await service.GetAsync(new OperationJournalPrincipal
            {
                OrganizationId = "org", WorkstreamIds = new[] { "parent" }
            }, "operation-1");
            Assert.AreEqual("operation-1", item.OperationId);
        }

        private sealed class RecordingStore : IOperationJournalStore
        {
            public int Calls { get; private set; }
            public int DetailsCalls { get; private set; }
            public Task<OperationJournalRecord> StartAsync(OperationJournalRecord x, CancellationToken ct = default)
            { Calls++; return Task.FromResult(x); }
            public Task<OperationJournalRecord> TransitionAsync(string o, string id, string expected, string next,
                string s, DateTimeOffset t, CancellationToken ct = default)
            { Calls++; return Task.FromResult(new OperationJournalRecord()); }
            public Task<OperationJournalRecord> RecoverAsync(string o, string id, string expected, string s,
                DateTimeOffset t, CancellationToken ct = default)
            { Calls++; return Task.FromResult(new OperationJournalRecord()); }
            public Task<OperationJournalDetail> AppendAsync(OperationJournalDetail x, CancellationToken ct = default)
            { Calls++; return Task.FromResult(x); }
            public Task<OperationJournalRecord> GetAsync(string o, string id, CancellationToken ct = default)
            { Calls++; return Task.FromResult(new OperationJournalRecord
                { OrganizationId = o, OperationId = id, WorkstreamId = "parent", WorkspaceId = "child" }); }
            public Task<OperationJournalPage<OperationJournalRecord>> ListAsync(OperationJournalScope s, int size,
                string cursor = null, CancellationToken ct = default)
            { Calls++; return Task.FromResult(new OperationJournalPage<OperationJournalRecord>()); }
            public Task<OperationJournalPage<OperationJournalDetail>> GetDetailsAsync(string o, string id, int size,
                string cursor = null, CancellationToken ct = default)
            { DetailsCalls++; return Task.FromResult(new OperationJournalPage<OperationJournalDetail>()); }
        }
    }
}
