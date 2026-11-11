using System;
using System.Collections.Generic;

namespace LagoVista.Relational.DefinitionRelease
{
    /// <summary>
    /// Typed read-only dependency projections for the Billing product catalog.
    /// Product writes must be performed by Billing's actual domain managers and
    /// repositories after the LIVE release coordinator authorizes a manifest.
    /// </summary>
    public sealed class ProductCatalogReleaseInspector
    {
        public ProductReleaseInspection InspectProduct(ProductDTO product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            var result = new ProductReleaseInspection
            {
                Id = product.Id.ToString(),
                Key = product.Key,
                Name = product.Name,
                CategoryId = product.ProductCategoryId.ToString(),
                UnitTypeId = product.UnitTypeId,
                RecurringCycleTypeId = product.RecurringCycleTypeId,
                UnitPrice = product.UnitPrice,
                UnitCost = product.UnitCost,
                Sku = product.Sku,
                ImageResourceId = product.ImageResourceId,
                ThumbnailImageResourceId = product.ThumbnailImageResourceId,
                RequiresCommercialApproval = true
            };

            if (product.Id == Guid.Empty)
                result.Blockers.Add("Product has no stable identity.");
            if (String.IsNullOrWhiteSpace(product.Key))
                result.Blockers.Add("Product key is missing.");
            if (product.ProductCategoryId == Guid.Empty)
                result.Blockers.Add("Product Category is required.");
            if (product.UnitTypeId <= 0)
                result.Blockers.Add("Product Unit Type is required.");
            if (product.RecurringCycleTypeId <= 0)
                result.Blockers.Add("Product Recurring Cycle Type is required.");
            if (String.IsNullOrWhiteSpace(product.Sku))
                result.Blockers.Add("Product SKU is required.");

            return result;
        }

        public ProductCategoryReleaseInspection InspectCategory(ProductCategoryDTO category)
        {
            if (category == null) throw new ArgumentNullException(nameof(category));

            var result = new ProductCategoryReleaseInspection
            {
                Id = category.Id.ToString(),
                Key = category.Key,
                Name = category.Name,
                CategoryTypeId = category.CategoryTypeId,
                CategoryTypeName = category.CategoryTypeName,
                ImageResourceId = category.ImageResourceId,
                ThumbnailImageResourceId = category.ThumbnailImageResourceId
            };

            if (category.Id == Guid.Empty)
                result.Blockers.Add("Product Category has no stable identity.");
            if (String.IsNullOrWhiteSpace(category.Key))
                result.Blockers.Add("Product Category key is missing.");
            if (String.IsNullOrWhiteSpace(category.CategoryTypeId) ||
                String.Equals(category.CategoryTypeId.Trim(), "-1", StringComparison.Ordinal))
                result.Blockers.Add("Product Category Type is required.");

            return result;
        }
    }

    public sealed class ProductReleaseInspection
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string CategoryId { get; set; }
        public int UnitTypeId { get; set; }
        public int RecurringCycleTypeId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public string Sku { get; set; }
        public string ImageResourceId { get; set; }
        public string ThumbnailImageResourceId { get; set; }
        public bool RequiresCommercialApproval { get; set; }
        public List<string> Blockers { get; set; } = new List<string>();
    }

    public sealed class ProductCategoryReleaseInspection
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string CategoryTypeId { get; set; }
        public string CategoryTypeName { get; set; }
        public string ImageResourceId { get; set; }
        public string ThumbnailImageResourceId { get; set; }
        public List<string> Blockers { get; set; } = new List<string>();
    }
}
