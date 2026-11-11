using LagoVista.Relational;
using LagoVista.Relational.DefinitionRelease;
using NUnit.Framework;
using System;

namespace LagoVista.Relational.Tests.DefinitionRelease
{
    [TestFixture]
    public sealed class ProductCatalogReleaseInspectorTests
    {
        [Test]
        public void Product_WithMissingCategory_UnitAndCycle_IsBlocked()
        {
            var inspected = new ProductCatalogReleaseInspector().InspectProduct(new ProductDTO
            {
                Id = Guid.NewGuid(),
                Key = "model-token",
                Name = "Model token",
                Sku = "MODEL-TOKEN",
                ProductCategoryId = Guid.Empty,
                UnitTypeId = 0,
                RecurringCycleTypeId = 0
            });
            Assert.That(inspected.Blockers, Has.Count.EqualTo(3));
            Assert.That(inspected.RequiresCommercialApproval, Is.True);
        }

        [Test]
        public void Product_WithResolvedReferenceIds_StillRequiresCommercialApproval()
        {
            var category = Guid.NewGuid();
            var inspected = new ProductCatalogReleaseInspector().InspectProduct(new ProductDTO
            {
                Id = Guid.NewGuid(),
                Key = "model-token",
                Name = "Model token",
                Sku = "MODEL-TOKEN",
                ProductCategoryId = category,
                UnitTypeId = 1,
                RecurringCycleTypeId = 1,
                UnitPrice = 3.5m,
                UnitCost = 1.0m,
                ImageResourceId = "IMAGE1"
            });
            Assert.That(inspected.Blockers, Is.Empty);
            Assert.That(inspected.CategoryId, Is.EqualTo(category.ToString()));
            Assert.That(inspected.RequiresCommercialApproval, Is.True);
            Assert.That(inspected.ImageResourceId, Is.EqualTo("IMAGE1"));
        }

        [Test]
        public void Category_WithoutValidCategoryType_IsBlocked()
        {
            var inspected = new ProductCatalogReleaseInspector().InspectCategory(new ProductCategoryDTO
            {
                Id = Guid.NewGuid(),
                Key = "tokens",
                Name = "Tokens",
                CategoryTypeId = "-1"
            });
            Assert.That(inspected.Blockers, Has.Count.EqualTo(1));
        }

        [Test]
        public void Category_ReportsItsActualCategoryTypeAndMedia()
        {
            var inspected = new ProductCatalogReleaseInspector().InspectCategory(new ProductCategoryDTO
            {
                Id = Guid.NewGuid(),
                Key = "tokens",
                Name = "Tokens",
                CategoryTypeId = "CATTYPE1",
                CategoryTypeName = "AI Usage",
                ImageResourceId = "IMAGE1"
            });
            Assert.That(inspected.Blockers, Is.Empty);
            Assert.That(inspected.CategoryTypeId, Is.EqualTo("CATTYPE1"));
            Assert.That(inspected.ImageResourceId, Is.EqualTo("IMAGE1"));
        }
    }
}
