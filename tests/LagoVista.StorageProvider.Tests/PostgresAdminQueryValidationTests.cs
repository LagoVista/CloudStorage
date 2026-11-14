using LagoVista.Relational.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LagoVista.StorageProvider.Tests
{
    [TestClass]
    public sealed class PostgresAdminQueryValidationTests
    {
        [TestMethod]
        public void SingleSelectIsAccepted()
        {
            Assert.AreEqual("SELECT 1", PostgresAdminRepo.ValidateReadOnlySql(" SELECT 1; "));
            Assert.AreEqual("WITH data AS (SELECT 2) SELECT * FROM data",
                PostgresAdminRepo.ValidateReadOnlySql("WITH data AS (SELECT 2) SELECT * FROM data"));
        }

        [TestMethod]
        public void MutationAndMultiStatementAreRejected()
        {
            foreach (var sql in new[] { "DELETE FROM things", "SELECT 1; DELETE FROM things", "UPDATE things SET id = 1", "SELECT 1;;" })
                Assert.ThrowsException<ArgumentException>(() => PostgresAdminRepo.ValidateReadOnlySql(sql));
        }

        [TestMethod]
        public void PageSizeIsBounded()
        {
            Assert.AreEqual(1, PostgresAdminRepo.BoundPageSize(0));
            Assert.AreEqual(100, PostgresAdminRepo.BoundPageSize(100));
            Assert.AreEqual(500, PostgresAdminRepo.BoundPageSize(10000));
        }
    }
}
