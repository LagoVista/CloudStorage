using LagoVista.Relational.DataContexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational.Tests.Core.Utils
{
    public class TestBillingDataContext
    {
        public static BillingDataContext CreateInMemoryContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<BillingDataContext>()
                .UseSqlite(connection)
                .Options;

            var db = new BillingDataContext(options);
            db.Database.EnsureCreated();
            return db;
        }
    }
}
