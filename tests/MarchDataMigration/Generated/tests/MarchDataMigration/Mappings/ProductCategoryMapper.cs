using MarchDataMigration.Generated.ProductCategory;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class ProductCategoryMapper
    {
        public static TargetProductCategoryRow Map(SourceProductCategoryRow source)
        {
            return new TargetProductCategoryRow
            {
                Id = source.Id,
                OrganizationId = source.OrgId,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
                Name = source.Name,
                Key = source.Key,
                Description = source.Description,
                IsPublic = source.IsPublic,
               // Icon = source.Icon,
                ThumbnailImageResourceId = source.ThumbnailImageResourceId,
                ThumbnailImageResourceName = source.ThumbnailImageResourceName,
                ImageResourceId = source.ImageResourceId,
                ImageResourceName = source.ImageResourceName,
                ShortSummaryHTML = source.ShortSummaryHTML,
                CategoryTypeId = source.CategoryTypeId,
                CategoryTypeName = source.CategoryTypeName,
            };
        }
    }
}
