using LagoVista.Relational;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Relational.Tests
{
    public sealed class BillingSchemaTests : SchemaContractTestBase
    {
        protected override DbContext CreateContextForTruthDb(string sqlServerConnectionString)
        {
            var opts = new DbContextOptionsBuilder<BillingDataContext>()
                .UseSqlServer(sqlServerConnectionString)
                .EnableSensitiveDataLogging()
                .Options;

            return new BillingDataContext(opts);
        }

        [Test]
        public Task AccountTransactionCategories() => AssertTableMatchesModelAsync(typeof(AccountTransactionCategoryDto), "dbo", "AccountTransactionCategory");


        [Test]
        public Task Agreements() => AssertTableMatchesModelAsync(typeof(AgreementDTO), "dbo", "Agreements");
      
        [Test]
        public Task AgreementLineItems() => AssertTableMatchesModelAsync(typeof(AgreementLineItemDTO), "dbo", "AgreementLineItems");

        
        [Test]
        public Task BillingEvents() => AssertTableMatchesModelAsync(typeof(BillingEventDTO), "dbo", "BillingEvents");


        [Test]
        public Task BudgetItems() => AssertTableMatchesModelAsync(typeof(BudgetItemDTO), "dbo", "BudgetItems");


        [Test]
        public Task Customers() => AssertTableMatchesModelAsync(typeof(CustomerDTO), "dbo", "Customers");

        

        [Test]
        public Task Expenses() => AssertTableMatchesModelAsync(typeof(ExpenseDTO), "dbo", "Expenses");

        [Test]
        public Task ExpenseCategory() => AssertTableMatchesModelAsync(typeof(ExpenseCategoryDTO), "dbo", "ExpenseCategory");

        [Test]
        public Task Invoices() => AssertTableMatchesModelAsync(typeof(InvoiceDTO), "dbo", "Invoice");

        [Test]
        public Task InvoiceLineItems() => AssertTableMatchesModelAsync(typeof(InvoiceLineItemDTO), "dbo", "InvoiceLineItems");

        [Test]
        public Task Payments() => AssertTableMatchesModelAsync(typeof(PaymentDTO), "dbo", "Payments");
        [Test]
        public Task PayRate() => AssertTableMatchesModelAsync(typeof(PayRateDTO), "dbo", "PayRates");
        [Test]
        public Task PayrollSummary() => AssertTableMatchesModelAsync(typeof(PayrollSummaryDTO), "dbo", "PayrollSummary");


        public Task Vendors() => AssertTableMatchesModelAsync(typeof(VendorDTO), "dbo", "Vendor");
        [Test]
        public Task WorkRole() => AssertTableMatchesModelAsync(typeof(WorkRoleDTO), "dbo", "WorkRoles");


        [Test]
        public Task TimeEntries() => AssertTableMatchesModelAsync(typeof(TimeEntryDTO), "dbo", "TimeEntries");


        [Test]
        public Task TimePeriods() => AssertTableMatchesModelAsync(typeof(TimePeriodDTO), "dbo", "TimePeriods");

    }
}
