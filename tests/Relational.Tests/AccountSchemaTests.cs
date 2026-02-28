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
    internal class AccountSchemaTests : SchemaContractTestBase
    {
        protected override DbContext CreateContextForTruthDb(string sqlServerConnectionString)
        {
            var opts = new DbContextOptionsBuilder<AccountDataContext>()
                .UseSqlServer(sqlServerConnectionString)
                .EnableSensitiveDataLogging()
                .Options;

            return new AccountDataContext(opts);
        }


        [Test]
        public Task Accounts() => AssertTableMatchesModelAsync(typeof(AccountDto), "dbo", "Account");


        [Test]
        public Task TransactionStaging() => AssertTableMatchesModelAsync(typeof(TransactionStagingDto), "dbo", "TransactionStaging");

        [Test]
        public Task AccountTransactions() => AssertTableMatchesModelAsync(typeof(AccountTransactionDto), "dbo", "AccountTransaction");


        [Test]
        public Task AccountTransactionCategory() => AssertTableMatchesModelAsync(typeof(AccountTransactionCategoryDto), "dbo", "AccountTransactionCategory");

    }
}
