using LagoVista.Relational;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relational.Tests
{
    public sealed class InvoiceSchemaTests : SchemaContractTestBase
    {
        protected override DbContext CreateContextForTruthDb(string sqlServerConnectionString)
        {
            var opts = new DbContextOptionsBuilder<InvoiceDataContext>()
                .UseSqlServer(sqlServerConnectionString)
                .EnableSensitiveDataLogging()
                .Options;

            return new InvoiceDataContext(opts);
        }

        [Test]
        public Task Agreements() => AssertTableMatchesModelAsync(typeof(AgreementDTO), "dbo", "Agreements");

        [Test]
        public Task AgreementLineItems() => AssertTableMatchesModelAsync(typeof(AgreementLineItemDTO), "dbo", "AgreementLineItems");



        [Test]
        public Task Invoices() => AssertTableMatchesModelAsync(typeof(InvoiceDTO), "dbo", "Invoice");


        [Test]
        public Task InvoicesLogs() => AssertTableMatchesModelAsync(typeof(InvoiceLogsDTO), "dbo", "InvoiceLogs");

        [Test]
        public Task InvoiceLineItems() => AssertTableMatchesModelAsync(typeof(InvoiceLineItemDTO), "dbo", "InvoiceLineItems");




        [Test]
        public Task Customers() => AssertTableMatchesModelAsync(typeof(CustomerDTO), "dbo", "Customers");
    }
}
