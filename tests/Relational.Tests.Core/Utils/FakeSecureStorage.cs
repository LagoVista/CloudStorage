using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using System;
using System.Threading.Tasks;

namespace Relational.Tests.Core.Utils
{
    public class FakeSecureStorage : ISecureStorage
    {
        public Task<InvokeResult<string>> AddSecretAsync(EntityHeader org, string id, string value)
        {
            return Task.FromResult(InvokeResult<string>.Create(id));
        }

        public Task<InvokeResult<string>> AddSecretAsync(EntityHeader org, string value)
        {
            return Task.FromResult(InvokeResult<string>.Create(Guid.NewGuid().ToId()));
        }

        public Task<InvokeResult<string>> AddUserSecretAsync(EntityHeader user, string value)
        {
            return Task.FromResult(InvokeResult<string>.Create(Guid.NewGuid().ToId()));
        }

        public Task<InvokeResult<string>> GetSecretAsync(EntityHeader org, string id, EntityHeader user)
        {
            return Task.FromResult(InvokeResult<string>.Create("1A9ACA7A85864C1B849058D95823EE93"));
        }

        public Task<InvokeResult<string>> GetUserSecretAsync(EntityHeader user, string id)
        {
            return Task.FromResult(InvokeResult<string>.Create("1A9ACA7A85864C1B849058D95823EE93"));
        }

        public Task<InvokeResult> RemoveSecretAsync(EntityHeader org, string id)
        {
            return Task.FromResult(InvokeResult.Success);
        }

        public Task<InvokeResult> RemoveUserSecretAsync(EntityHeader user, string id)
        {
            return Task.FromResult(InvokeResult.Success);
        }
    }
}
