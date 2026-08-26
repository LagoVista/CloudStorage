using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.DocumentStorage
{
    internal static partial class DocumentStorageClientContract
    {
        public static async Task SummaryQueryAsync(IDocumentStorageClient client)
        {
            var categoryA = EntityHeader.Create("CAT-A", "Category A");
            var categoryB = EntityHeader.Create("CAT-B", "Category B");

            var alpha = CreateSummaryEntity("Alpha", categoryA);
            var bravo = CreateSummaryEntity("Bravo", categoryA);
            var charlie = CreateSummaryEntity("Charlie", categoryA);
            var otherCategory = CreateSummaryEntity("Delta", categoryB);
            var deleted = CreateSummaryEntity("Deleted", categoryA);
            deleted.IsDeleted = true;
            var draft = CreateSummaryEntity("Draft", categoryA);
            draft.IsDraft = true;

            await client.CreateDocumentAsync(alpha);
            await client.CreateDocumentAsync(bravo);
            await client.CreateDocumentAsync(charlie);
            await client.CreateDocumentAsync(otherCategory);
            await client.CreateDocumentAsync(deleted);
            await client.CreateDocumentAsync(draft);

            var firstPage = await client.QuerySummaryAsync<ContractSummaryEntity>(
                nameof(ContractSummaryEntity),
                item => item.OwnerOrganization.Id == "ORG1",
                item => item.Name,
                new ListRequest { PageIndex = 1, PageSize = 2, CategoryKey = "CAT-A" },
                descending: false);

            Assert.AreEqual(2, firstPage.Model.Count());
            CollectionAssert.AreEqual(new[] { "Alpha", "Bravo" }, firstPage.Model.Select(item => item.Name).ToArray());

            var secondPage = await client.QuerySummaryAsync<ContractSummaryEntity>(
                nameof(ContractSummaryEntity),
                item => item.OwnerOrganization.Id == "ORG1",
                item => item.Name,
                new ListRequest { PageIndex = 2, PageSize = 2, CategoryKey = "CAT-A" },
                descending: false);

            Assert.AreEqual(1, secondPage.Model.Count());
            Assert.AreEqual("Charlie", secondPage.Model.Single().Name);

            var includeHidden = await client.QuerySummaryAsync<ContractSummaryEntity>(
                nameof(ContractSummaryEntity),
                item => item.OwnerOrganization.Id == "ORG1",
                item => item.Name,
                new ListRequest
                {
                    PageIndex = 1,
                    PageSize = 10,
                    CategoryKey = "CAT-A",
                    ShowDeleted = true,
                    ShowDrafts = true
                },
                descending: true);

            CollectionAssert.AreEquivalent(
                new[] { "Alpha", "Bravo", "Charlie", "Deleted", "Draft" },
                includeHidden.Model.Select(item => item.Name).ToArray());
            Assert.AreEqual("Draft", includeHidden.Model.First().Name);
        }

        private static ContractSummaryEntity CreateSummaryEntity(string name, EntityHeader category)
        {
            var id = Guid.NewGuid().ToString("N").ToUpperInvariant();
            return new ContractSummaryEntity
            {
                Id = id,
                Key = $"summary-{id.ToLowerInvariant()}",
                Name = name,
                EntityType = nameof(ContractSummaryEntity),
                OwnerOrganization = EntityHeader.Create("ORG1", "Contract Org"),
                Category = category
            };
        }
    }

    internal sealed class ContractSummaryEntity : EntityBase, ISummaryFactory, ICategorized
    {
        public ContractSummaryData CreateSummary()
        {
            return new ContractSummaryData
            {
                Id = Id,
                Key = Key,
                Name = Name
            };
        }

        ISummaryData ISummaryFactory.CreateSummary() => CreateSummary();
    }

    internal sealed class ContractSummaryData : SummaryData
    {
    }
}
