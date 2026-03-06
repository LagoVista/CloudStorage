using LagoVista.Relational.DataContexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relational.Tests
{
    public class SQLitProviderTests
    {
        [Test]
        public async Task CreateSimpleSQLiteSchema()
        {
            var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");
            await conn.OpenAsync().ConfigureAwait(false);


            var builder = new DbContextOptionsBuilder<SimpleDataContext>();
            var options = builder.UseSqlite(conn);

            var ctx = new SimpleDataContext(options.Options);
            var sql = ctx.Database.GenerateCreateScript();
            Console.WriteLine(sql);
        }

        [Test]
        public async Task Create_FullBillingContext_Schema()
        {
            var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");
            await conn.OpenAsync().ConfigureAwait(false);


            var builder = new DbContextOptionsBuilder<BillingDataContext>();
            var options = builder.UseSqlite(conn);

            var ctx = new BillingDataContext(options.Options);
            var sql = ctx.Database.GenerateCreateScript();
            Console.WriteLine(sql);
        }
    }
}
