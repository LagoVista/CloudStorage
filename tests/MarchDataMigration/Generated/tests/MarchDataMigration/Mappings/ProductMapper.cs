using MarchDataMigration.Generated.Product;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class ProductMapper
    {
        public static TargetProductRow Map(SourceProductRow source)
        {
            return new TargetProductRow
            {
                Id = source.Id,
                ProductCategoryId = source.ProductCategoryId,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
                Key = source.Key,
                Name = source.Name,
                Sku = source.Sku,
                Status = source.Status,
                UnitCost = source.UnitCost,
                UnitTypeId = source.UnitTypeId,
                Description = source.Description,
                DetailsHTML = source.DetailsHTML,
                RemoteResourceId = source.RemoteResourceId,
                IsTrialResource = source.IsTrialResource,
                //Icon = source.Icon,
                ThumbnailImageResourceId = source.ThumbnailImageResourceId,
                ThumbnailImageResourceName = source.ThumbnailImageResourceName,
                ImageResourceId = source.ImageResourceId,
                ImageResourceName = source.ImageResourceName,
                PhysicalProduct = source.PhysicalProduct,
                ShortSummaryHTML = source.ShortSummaryHTML,
                UnitPrice = source.UnitPrice,
                IsPublic = source.IsPublic,
                RecurringCycleTypeId = source.RecurringCycleTypeId,
            };
        }
    }
}
