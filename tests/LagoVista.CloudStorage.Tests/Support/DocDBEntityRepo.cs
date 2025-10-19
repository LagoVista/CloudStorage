// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 398cf5d37b8c4dc1da2ada5f119775a3acb4b012238afd0ff90cab370a46f83f
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public Task<ListResponse<DocDBEntitty>> GetOrderedQuery(ListRequest rqst)
        {
            return QueryAsync(doc => true, doc =>  doc.Name, rqst);
        }


        public Task<ListResponse<DocDbSummary>> GetOrderedSummaryQuery(ListRequest rqst)
        {
            return QuerySummaryAsync<DocDbSummary, DocDBEntitty>(doc => true, doc => doc.Name, rqst);
        }

        public Task<ListResponse<DocDBEntitty>> GetForOrgDescAsync(string orgid, ListRequest rqst)
        {
            return DescOrderQueryAsync(doc => doc.OwnerOrganization!.Id == orgid, doc=>doc.Index, rqst);
        }

        public async Task<List<TObj>> GetDocumentSummary<TObj>()
        {
            var sql = "SELECT root.id, root.TaskCode, root.Name, root.Status.Id StatusId, root.Status.Text Status from root where root.EntityType = 'WorkTask' and root.Project.Id = '69B0D6F5731D48B9AB0380092BF82339'";
            var container = await GetContainerAsync();

            var query = new QueryDefinition(sql);

            //foreach (var param in sqlParams)
            //{
            //    query = query.WithParameter(param.Name, param.Value);
            //}

            var sw = Stopwatch.StartNew();

            var items = new List<TObj>();

            using (var resultSet = container.GetItemQueryIterator<TObj>(query))
            {
                var page = 1;
                while (resultSet.HasMoreResults)
                {
                    var response = await resultSet.ReadNextAsync();
                    Console.WriteLine($"[DocStorage] Page {page++} Query Document {sql} => {sw.Elapsed.TotalMilliseconds}ms, Request Charge: {response.RequestCharge}");
                    items.AddRange(response);
                }
            }

            return items;
        }


        public Task<IEnumerable<DocDBEntitty>> GetForOrgWithSQL(string orgId)
        {
            var query = $"select * from c where c.OwnerOrganization.Id = @orgid";
            return QueryAsync(query, new QueryParameter("@orgid", orgId));
        }
    }
}
