using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Data.Common;
using System.Threading.Tasks;


namespace Relational.Tests.Core.Database
{
    public sealed class TestDbContextFactory<TContext> where TContext : DbContext
    {
        private readonly Func<DbContextOptions<TContext>, TContext> _contextCtor;

        public bool SetupCompleted { get; set; }

        BillingDataContext _userAdminContext;

        public TestDbContextFactory(Func<DbContextOptions<TContext>, TContext> contextCtor)
        {
            _contextCtor = contextCtor ?? throw new ArgumentNullException(nameof(contextCtor));
        }

        public TContext Create(EfTestDatabase db, bool enableSensitiveLogging = true)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));

            var builder = new DbContextOptionsBuilder<TContext>();
            var uaBuilder = new DbContextOptionsBuilder<BillingDataContext>();

            if (enableSensitiveLogging)
            {
                builder.AddInterceptors(new CommandSnifferInterceptor());
                builder.EnableSensitiveDataLogging().EnableDetailedErrors().LogTo(
                    s =>
                    {
                        if (SetupCompleted)
                            TestContext.Out.WriteLine(s);
                    },
                    [
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting,
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError
                    ]);

                uaBuilder.AddInterceptors(new CommandSnifferInterceptor());
                uaBuilder.EnableSensitiveDataLogging().EnableDetailedErrors().LogTo(
                    s =>
                    {
                        if (SetupCompleted)
                            TestContext.Out.WriteLine(s);
                    },
                    [
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting,
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError
                    ]);
            }

            // Provider wiring
            switch (db.Provider)
            {
                case EfTestProvider.SqliteInMemory:
                    builder.UseSqlite(db.SharedConnection!); // use the OPEN connection
                    uaBuilder.UseSqlite(db.SharedConnection!);
                    break;

                case EfTestProvider.SqlServer:
                    builder.UseSqlServer(db.ConnectionString);
                    uaBuilder.UseSqlServer(db.ConnectionString);
                    break;

                case EfTestProvider.Postgres:
                    builder.UseNpgsql(db.ConnectionString);
                    uaBuilder.UseNpgsql(db.ConnectionString);
                    break;

                default:
                    throw new NotSupportedException($"Unknown provider: {db.Provider}");
            }

            _userAdminContext = new BillingDataContext(uaBuilder.Options);
            return _contextCtor(builder.Options);
        }

        public bool Diagnostics { get; set; } = false;

        public async Task InitializeAsync(TContext ctx, bool useMigrations = false)
        {
            try
            {
                if (Diagnostics) TestContext.Out.WriteLine("--------------------------------------------------");
                if (Diagnostics) TestContext.Out.WriteLine($"Init Context {ctx.GetType().Name} - User Migration {useMigrations}");

                if (useMigrations)
                {
                    await _userAdminContext.Database.MigrateAsync();
                    await ctx.Database.MigrateAsync().ConfigureAwait(false);
                }
                else
                {
                    //await _userAdminContext.Database.EnsureCreatedAsync();
                    await ctx.Database.EnsureCreatedAsync().ConfigureAwait(false);
                }

                if (Diagnostics)
                {
                    using var cmd = ctx.Database.GetDbConnection()!.CreateCommand();
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                        TestContext.Out.WriteLine("TABLE: " + rdr.GetString(0));
                }
                if(Diagnostics) TestContext.Out.WriteLine("--------------------------------------------------");

            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine("Error during database initialization:");
                TestContext.Out.WriteLine(ex.ToString());
                throw;
            }


            SetupCompleted = true;
        }

        public async Task ResetAsync(TContext ctx)
        {
            // Fast + deterministic: drop & recreate schema.
            // If you prefer, you can delete rows instead, but drop/recreate avoids state bugs.
            await ctx.Database.EnsureDeletedAsync().ConfigureAwait(false);
            await ctx.Database.MigrateAsync().ConfigureAwait(false);
        }
    }


    public sealed class CommandSnifferInterceptor : DbCommandInterceptor
    {
        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            TestContext.Out.WriteLine("EF COMMAND FAILED:");
            TestContext.Out.WriteLine(command.CommandText);
            foreach (DbParameter param in command.Parameters)
            {
                TestContext.Out.WriteLine($"\t\t{param.ParameterName} = {param.Value} (DbType: {param.DbType})");
            }   
            base.CommandFailed(command, eventData);
        }
    }
}
