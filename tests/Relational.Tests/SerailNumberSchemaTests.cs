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
    public class SerailNumberSchemaTests : SchemaContractTestBase
    {
        protected override DbContext CreateContextForTruthDb(string sqlServerConnectionString)
        {
            var opts = new DbContextOptionsBuilder<SerialNumberDataContext>()
                .UseSqlServer(sqlServerConnectionString)
                .EnableSensitiveDataLogging()
                .Options;

            return new SerialNumberDataContext(opts);
        }


        [Test]
        public Task SerialNumbers() => AssertTableMatchesModelAsync(typeof(SerialNumberDTO), "dbo", "SerialNumbers");



    }
}
