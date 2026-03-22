using MarchDataMigration.Generated.ProductIncluded;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class ProductIncludedMapper
    {
        public static TargetProductIncludedRow Map(SourceProductIncludedRow source)
        {
            return new TargetProductIncludedRow
            {
                Id = source.Id,
                PackageId = source.PackageId,
                ProductId = source.ProductId,
                DiscountPercent = source.DIscount,
                Notes = source.Notes,
                Name = source.Name,
                Key = source.Key,
                Quantity = source.Quantity,
            };
        }
    }
}
