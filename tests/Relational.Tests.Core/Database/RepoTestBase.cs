using LagoVista.Core.AutoMapper;
using LagoVista.Core.AutoMapper.Converters;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Interfaces.AutoMapper;
using LagoVista.Core.Models;
using LagoVista.Core.Services;
using LagoVista.Core.Services.Crypto;
using LagoVista.Crypto.Modern;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using LagoVista.Relational.DataContexts;
using LagoVista.Relational.Services;
using Microsoft.EntityFrameworkCore;
using Relational.Tests.Core.Seeds;
using System;
using System.Threading.Tasks;


namespace Relational.Tests.Core.Database
{
    public abstract class RepoTestBase
    {
        protected TestEnvironment Env { get; private set; } = default!;
        protected BillingDataContext BillingCtx { get; private set; }

        public BillingDataContext Ctx1 { get; private set; }
        protected MetricsDataContext MetricsCtx => Env.Metrics;
     
        protected IAdminLogger Logger { get; } = new AdminLogger(new ConsoleLogWriter());
        protected ISecureStorage SecureStorage { get; } = new FakeSecureStorage();
        protected ILagoVistaAutoMapper AutoMapper { get; private set; } = default!;

        protected EntityHeader UserEH { get; private set; }
        protected EntityHeader OrgEH { get; private set; }

        protected abstract EfTestProvider Provider { get; }

        protected abstract void CreateRepos(BillingDataContext billing, MetricsDataContext metricsDataContext, IAdminLogger logger, ILagoVistaAutoMapper autoMapper, ISecureStorage secureStorage);

        protected virtual string SqlServerBaseConnectionString => "Server=localhost,1433;Database=master;User Id=sa;Password=4SomeDbTesting?;Encrypt=True;TrustServerCertificate=True;";
        protected virtual string PostgresBaseConnectionString => Environment.GetEnvironmentVariable("TEST_POSTGRES_CS") ?? "";

        protected EfTestDatabase TheDb { get; private set; } = default!;

        PrettyCommandInterceptor cmdInterceptor = new PrettyCommandInterceptor();

        protected abstract Task PopulateAsync();

        [SetUp]
        public async Task SetUp()
        {
            cmdInterceptor.IsReady = false;
           
            // Core Primaries for Evenrything
            UserSeeds.Populate(10);
            OrganizationSeeds.Populate(10);




            UserEH = UserSeeds.Primary.ToEntityHeader();
            OrgEH = OrganizationSeeds.Primary.ToEntityHeader();

          
            var baseCs = Provider switch
            {
                EfTestProvider.SqlServer => SqlServerBaseConnectionString,
                EfTestProvider.Postgres => PostgresBaseConnectionString,
                _ => null
            };

            TheDb = await EfTestDatabase.CreateAsync(Provider, baseCs).ConfigureAwait(false);

            var enableSensitiveLogging = true;
            BillingCtx = new BillingDataContext(CreateContext<BillingDataContext>(TheDb, enableSensitiveLogging).Options);
            Ctx1 = new BillingDataContext(CreateContext<BillingDataContext>(TheDb, enableSensitiveLogging).Options);
            var metrics = new MetricsDataContext(CreateContext<MetricsDataContext>(TheDb, enableSensitiveLogging).Options);

            AutoMapper = CreateAutoMapper(SecureStorage);
            Env = new TestEnvironment(BillingCtx, metrics, Logger, AutoMapper, SecureStorage);

            await InitializeAsync(BillingCtx);
            await InitializeAsync(metrics);
            
            CreateRepos(BillingCtx, metrics, Logger, AutoMapper, SecureStorage);

            await PopulateAsync();

            cmdInterceptor.IsReady = true;

            TestContext.WriteLine($"Running test with provider: {Provider}");
            TestContext.WriteLine("======================================");
        }

        protected void HideConsole()
        {
            cmdInterceptor.IsReady = false;
        }

        protected void ShowConsole()
        {
            cmdInterceptor.IsReady = true;
        }   


        [TearDown]
        public async Task TearDown()
        {
            if (Env is not null)
                await Env.DisposeAsync().ConfigureAwait(false);

            if (TheDb is not null)
                await TheDb.DisposeAsync().ConfigureAwait(false);
        }

        private DbContextOptionsBuilder<TContext> CreateContext<TContext>(EfTestDatabase db, bool enableSensitiveLogging) where TContext : DbContext
        {
            var builder = new DbContextOptionsBuilder<TContext>();
            if (db.Provider == EfTestProvider.SqliteInMemory)
                builder.UseSqlite(db.SharedConnection!);
            else if (db.Provider == EfTestProvider.SqlServer)
                builder.UseSqlServer(db.ConnectionString);
            else if (db.Provider == EfTestProvider.Postgres)
                builder.UseNpgsql(db.ConnectionString);
            if (enableSensitiveLogging)
                builder
                    .AddInterceptors(cmdInterceptor)
                    .EnableSensitiveDataLogging()
                    .AddInterceptors()
                    .EnableDetailedErrors()
                    .LogTo(s =>
                    {
                        //           if(_initCompleted)
                        //             TestContext.Out.WriteLine(s);
                    }, new[]
                    {
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting,
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError
                    });
            return builder;
        }

        protected virtual Task InitializeAsync(DbContext ctx) => new TestDbContextFactory<DbContext>(_ => (DbContext)ctx).InitializeAsync(ctx, useMigrations: false);

        protected virtual ILagoVistaAutoMapper CreateAutoMapper(ISecureStorage secureStorage)
        {
            var registry = ConvertersRegistration.DefaultConverterRegistery;
            var atomicBuilder = new ReflectionAtomicPlanBuilder(registry);
            var keyProvider = new EncryptionKeyProvider(secureStorage);
            var planner = new EncryptedMapperPlanner(registry);
            var encryptor = new Encryptor();
            var modernEncryptor = new ModernEncryptionService(new AadBuilderV1(), new EnvelopeCodecV2(), new SecureStorageKeyMaterialStore(secureStorage), new AesGcmEncryptorNet9(), OrgEH, UserEH);

            var encryptedMapper = new EncryptedMapper(keyProvider, planner, encryptor, modernEncryptor, new ModernKeyIdBuilder(new KeyIdTargetResolver(BillingCtx)));
            return new LagoVistaAutoMapper(encryptedMapper, atomicBuilder, registry);
        }
    }
}
