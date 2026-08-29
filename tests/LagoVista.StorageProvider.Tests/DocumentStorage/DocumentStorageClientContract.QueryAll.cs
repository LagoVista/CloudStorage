using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.DocumentStorage
{
    internal static partial class DocumentStorageClientContract
    {
        public static async Task QueryAllAsync(IDocumentStorageClient client)
        {
            var alpha = CreateEntity("all", "Alpha");
            var bravo = CreateEntity("all", "Bravo");
            bravo.IsDeleted = true;
            var charlie = CreateEntity("all", "Charlie");
            charlie.IsDraft = true;

            await client.CreateDocumentAsync(alpha);
            await client.CreateDocumentAsync(bravo);
            await client.CreateDocumentAsync(charlie);

            var all = await client.QueryAllAsync<ContractDocumentEntity>(item => item.Detail == "all", new ListRequest { PageIndex = 1, PageSize = 10 });
            CollectionAssert.AreEquivalent(new[] { "Alpha", "Bravo", "Charlie" }, all.Model.Select(item => item.Name).ToArray());

            var firstPageDescending = await client.QueryAllAsync<ContractDocumentEntity, string>(item => item.Detail == "all", item => item.Name, new ListRequest { PageIndex = 1, PageSize = 2 }, descending: true);
            CollectionAssert.AreEqual(new[] { "Charlie", "Bravo" }, firstPageDescending.Model.Select(item => item.Name).ToArray());

            var secondPageDescending = await client.QueryAllAsync<ContractDocumentEntity, string>(item => item.Detail == "all", item => item.Name, new ListRequest { PageIndex = 2, PageSize = 2 }, descending: true);
            Assert.AreEqual(1, secondPageDescending.Model.Count());
            Assert.AreEqual("Alpha", secondPageDescending.Model.Single().Name);
        }
    }
}
