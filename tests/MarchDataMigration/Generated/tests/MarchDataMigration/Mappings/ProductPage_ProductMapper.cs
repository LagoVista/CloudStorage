using MarchDataMigration.Generated.ProductPage_Product;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class ProductPage_ProductMapper
    {
        public static TargetProductPage_ProductRow Map(SourceProductPage_ProductRow source)
        {
            return new TargetProductPage_ProductRow
            {
                Id = source.Id,
                ProductPageId = source.ProductPageId,
                ProductId = source.ProductId,
                Discount = source.Discount,
                Index = source.Index,
                UnitQty = source.UnitQty,
            };
        }
    }
}
