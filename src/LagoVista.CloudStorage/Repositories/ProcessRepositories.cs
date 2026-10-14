using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Repositories
{
    public sealed class ProcessDefinitionRepository : DocumentDBRepoBase<ProcessDefinition>, IProcessDefinitionRepository
    {
        public ProcessDefinitionRepository(IDocumentCloudServices cloudServices) : base(cloudServices)
        {
            if (cloudServices == null) throw new ArgumentNullException(nameof(cloudServices));
        }

        public async Task AddAsync(ProcessDefinition definition)
        {
            await CreateDocumentAsync(definition);
        }

        public Task<ProcessDefinition> GetAsync(string id)
        {
            return GetDocumentAsync(id, false);
        }

        public async Task UpdateAsync(ProcessDefinition definition)
        {
            await UpsertDocumentAsync(definition);
        }
    }

    public sealed class ProcessInstanceRepository : DocumentDBRepoBase<ProcessInstance>, IProcessInstanceRepository
    {
        protected override bool IsRuntimeData => true;

        public ProcessInstanceRepository(IDocumentCloudServices cloudServices) : base(cloudServices)
        {
            if (cloudServices == null) throw new ArgumentNullException(nameof(cloudServices));
        }

        public async Task AddAsync(ProcessInstance instance)
        {
            await CreateDocumentAsync(instance);
        }

        public Task<ProcessInstance> GetAsync(string id)
        {
            return GetDocumentAsync(id, false);
        }

        public async Task UpdateAsync(ProcessInstance instance)
        {
            await UpsertDocumentAsync(instance);
        }
    }
}
