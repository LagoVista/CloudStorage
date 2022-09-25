using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests.Support
{
    public class DocDBEntityRepo : DocumentDB.DocumentDBRepoBase<DocDBEntitty>
    {
        public DocDBEntityRepo(Uri endpoint, string sharedKey, string dbName, IAdminLogger logger, ICacheProvider cacheProvider = null) : base(endpoint, sharedKey, dbName, logger, cacheProvider)
        {

        }

        protected override bool ShouldConsolidateCollections => true;

        public Task CreateDBDocment(DocDBEntitty entity)
        {
            return CreateDocumentAsync(entity);
        }

        public Task<DocDBEntitty> GetDocDBEntitty(string id)
        {
            return GetDocumentAsync(id);
        }

        public Task DeleteDocDBEntity(string id)
        {
            return DeleteDocumentAsync(id);
        }

        public Task UpdateDBDocument(DocDBEntitty entity)
        {
            return UpsertDocumentAsync(entity);
        }

        public Task<IEnumerable<DocDBEntitty>> GetForOrgAsync(string orgid)
        {
            return QueryAsync(doc => doc.OwnerOrganization!.Id == orgid);
        }

        public Task<ListResponse<DocDBEntitty>> GetForOrgAsync(string orgid, ListRequest rqst)
        {
            return QueryAsync(doc => doc.OwnerOrganization!.Id == orgid, rqst);
        }

        public Task<ListResponse<DocDBEntitty>> GetForOrgAllAsync(string orgid, ListRequest rqst)
        {
            return QueryAllAsync(doc => doc.OwnerOrganization!.Id == orgid, rqst);
        }

        public Task<ListResponse<DocDBEntitty>> GetForOrgDescAsync(string orgid, ListRequest rqst)
        {
            return DescOrderQueryAsync(doc => doc.OwnerOrganization!.Id == orgid, doc=>doc.Index, rqst);
        }


        public Task<IEnumerable<DocDBEntitty>> GetForOrgWithSQL(string orgId)
        {
            var query = $"select * from c where c.OwnerOrganization.Id = @orgid";
            return QueryAsync(query, new QueryParameter("@orgid", orgId));
        }
    }
}
