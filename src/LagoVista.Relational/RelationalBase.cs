using LagoVista.Core.Encryption;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
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
        TContext _context;
        ISecureStorage _secureStorage;
        ILogger _adminlogger;

        public RelationalBase(TContext context, ILogger adminLogger, ISecureStorage secureStorage)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _adminlogger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
        }

        public async Task AddWithContextAsync(EntityHeader org, EntityHeader user, string secretId, Func<EncryptedRepoContext<TContext>, Task> work)
        {
            var encryptionServices = new EncryptionServices(_secureStorage, _adminlogger, secretId, org, user);
            var svc = new EncryptedRepoContext<TContext>(_context, encryptionServices);
            await svc.EncryptionServices.SetAccountEncryptionString();
            await work(svc);
        }

        public async Task UpdateWithContextAsync(EntityHeader org, EntityHeader user, string secretId, Func<EncryptedRepoContext<TContext>, Task> work)
        {
            var encryptionServices = new EncryptionServices(_secureStorage, _adminlogger, secretId, org, user);
            var svc = new EncryptedRepoContext<TContext>(_context, encryptionServices);
            await svc.EncryptionServices.SetAccountEncryptionString();
            await work(svc);
        }


        public async Task AddWithContextAsync(EntityHeader org, EntityHeader user, Func<TContext, Task> work)
        {
            await work(_context);
        }

        public async Task UpdateWithContextAsync(EntityHeader org, EntityHeader user, Func<TContext, Task> work)
        {
            await work(_context);
        }

        public async Task DeleteWithContextAsync(EntityHeader org, EntityHeader user, Func<TContext, Task> work)
        {
            await work(_context);
        }

        public async Task<TResult> WithContextAsync<TResult>(EntityHeader org, EntityHeader user, string secretId, Func<EncryptedRepoContext<TContext>, Task<TResult>> work)
        {
            var encryptionServices = new EncryptionServices(_secureStorage, _adminlogger, secretId, org, user);
            var svc = new EncryptedRepoContext<TContext>(_context, encryptionServices);
            await svc.EncryptionServices.SetAccountEncryptionString();
            return await work(svc);
        }

        public async Task<TResult> WithContextAsync<TResult>(EntityHeader org, EntityHeader user, Func<TContext, Task<TResult>> work)
        {
            return await work(_context);
        }

        public async Task CommitAsync()
        {
            _context.SaveChanges();
        }
    }
}
