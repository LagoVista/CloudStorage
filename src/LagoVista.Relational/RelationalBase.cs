using LagoVista.Core.Encryption;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class EncryptedRepoContext<TContext> where TContext : DbContext
    {
        public EncryptedRepoContext(TContext db, IEncryptionServices encryptionServices)
        {
            Db = db ?? throw new ArgumentNullException(nameof(db));
            EncryptionServices = encryptionServices ?? throw new ArgumentNullException(nameof(encryptionServices));
        }

        public IEncryptionServices EncryptionServices { get; }

        public TContext Db { get; }
    }

    public class RelationalBase<TContext> where TContext : DbContext
    {
        ISecureStorage _secureStorage;
        ILogger _adminlogger;

        private readonly IDbContextFactory<TContext> _contextFactory;



        public RelationalBase(IDbContextFactory<TContext> factory, ILogger adminLogger, ISecureStorage secureStorage)
        {
            _contextFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            _adminlogger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
        }

        protected TContext CreateContext()
        {
            var context = _contextFactory.CreateDbContext();
            if(context == null)
            {
                throw new InvalidOperationException("Could not create instance of DbContext.");
            }
            return context;
        }

        protected async Task AddWithContextAsync(EntityHeader org, EntityHeader user, string secretId, Func<EncryptedRepoContext<TContext>, Task> work)
        {
            try
            {
                await using var context = CreateContext();
                var encryptionServices = new EncryptionServices(_secureStorage, _adminlogger, secretId, org, user);
                var svc = new EncryptedRepoContext<TContext>(context, encryptionServices);
                await svc.EncryptionServices.SetAccountEncryptionString();
                await work(svc);
            }
            catch (Exception ex)
            {
                _adminlogger.AddException(this.Tag(), ex);
                throw;
            }
        }

        protected async Task UpdateWithContextAsync(EntityHeader org, EntityHeader user, string secretId, Func<EncryptedRepoContext<TContext>, Task> work)
        {
            await using var context = CreateContext();
            var encryptionServices = new EncryptionServices(_secureStorage, _adminlogger, secretId, org, user);
            var svc = new EncryptedRepoContext<TContext>(context, encryptionServices);
            await svc.EncryptionServices.SetAccountEncryptionString();
            await work(svc);
        }


        protected async Task AddWithContextAsync(EntityHeader org, EntityHeader user, Func<TContext, Task> work)
        {
            await using var context = CreateContext();
            await work(context);
        }

        protected async Task UpdateWithContextAsync(EntityHeader org, EntityHeader user, Func<TContext, Task> work)
        {
            await using var context = CreateContext();
            await work(context);
        }

        protected async Task DeleteWithContextAsync(EntityHeader org, EntityHeader user, Func<TContext, Task> work)
        {
            await using var context = CreateContext();
            await work(context);
        }

        protected async Task<TResult> WithContextTransactionAsync<TResult>(EntityHeader org, EntityHeader user, string secretId, Func<EncryptedRepoContext<TContext>, Task<TResult>> work)
        {
            await using var rootContext = CreateContext();
            var strategy = rootContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var workerContext = CreateContext();
                var encryptionServices = new EncryptionServices(_secureStorage, _adminlogger, secretId, org, user);

                await using var tx = await workerContext.Database.BeginTransactionAsync();
                try
                {
                    var svc = new EncryptedRepoContext<TContext>(workerContext, encryptionServices);
                    await svc.EncryptionServices.SetAccountEncryptionString();
                    var result = await work(svc);
                    await tx.CommitAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    try
                    {
                        await tx.RollbackAsync().ConfigureAwait(false);
                    }
                    catch (Exception rollbackEx)
                    {
                        _adminlogger.AddException(this.Tag(), rollbackEx);
                    }

                    if (ex.InnerException != null)
                        _adminlogger.AddException(this.Tag(), ex.InnerException);

                    _adminlogger.AddException(this.Tag(), ex);
                    throw;
                }
            });
        }

        protected async Task<TResult> RetryOnConcurrencyAsync<TResult>(Func<Task<TResult>> work, int maxAttempts = 3)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));
            if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await work().ConfigureAwait(false);
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    _adminlogger.AddCustomEvent(LogLevel.Warning, this.Tag(), $"Concurrency conflict detected on attempt {attempt}. Retrying...");

                    // Small backoff helps reduce repeated collisions
                    await Task.Delay(Random.Shared.Next(5, 25) * attempt);
                }
            }

            throw new InvalidOperationException("Unreachable.");
        }

        protected async Task<TResult> WithContextTransactionAsync<TResult>(EntityHeader org, EntityHeader user, Func<TContext, Task<TResult>> work)
        {
            await using var rootContext = CreateContext();
            var strategy = rootContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var workerContext = CreateContext();
                await using var tx = await workerContext.Database.BeginTransactionAsync();
                    
                try
                {
                    var result = await work(workerContext);
                    await tx.CommitAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    try
                    {
                        await tx.RollbackAsync().ConfigureAwait(false);
                    }
                    catch (Exception rollbackEx)
                    {
                        _adminlogger.AddException(this.Tag(), rollbackEx);
                    }

                    if (ex.InnerException != null)
                        _adminlogger.AddException(this.Tag(), ex.InnerException);

                    _adminlogger.AddException(this.Tag(), ex);
                    throw;
                }
            });
        }

        protected async Task WithContextTransactionAsync(EntityHeader org, EntityHeader user, Func<TContext, Task> work)
        {
            await using var rootContext = CreateContext();
            var strategy = rootContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var workerContext = CreateContext();
                await using var tx = await workerContext.Database.BeginTransactionAsync();

                try
                {
                    await work(workerContext);
                    await tx.CommitAsync();
                 }
                catch (Exception ex)
                {
                    try
                    {
                        await tx.RollbackAsync().ConfigureAwait(false);
                    }
                    catch (Exception rollbackEx)
                    {
                        _adminlogger.AddException(this.Tag(), rollbackEx);
                    }

                    if (ex.InnerException != null)
                        _adminlogger.AddException(this.Tag(), ex.InnerException);

                    _adminlogger.AddException(this.Tag(), ex);
                    throw;
                }
            });

        }

        protected async Task<TResult> WithContextAsync<TResult>(EntityHeader org, EntityHeader user, string secretId, Func<EncryptedRepoContext<TContext>, Task<TResult>> work)
        {
            await using var context = CreateContext();
            var encryptionServices = new EncryptionServices(_secureStorage, _adminlogger, secretId, org, user);
            var svc = new EncryptedRepoContext<TContext>(context, encryptionServices);
            try
            {
                await svc.EncryptionServices.SetAccountEncryptionString();
                return await work(svc);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    _adminlogger.AddException(this.Tag(), ex.InnerException);
                _adminlogger.AddException(this.Tag(), ex);

                throw;
            }
        }

        protected async Task<TResult> WithContextAsync<TResult>(EntityHeader org, EntityHeader user, Func<TContext, Task<TResult>> work)
        {
            await using var context = CreateContext();
            return await work(context);
        }
    }
}
