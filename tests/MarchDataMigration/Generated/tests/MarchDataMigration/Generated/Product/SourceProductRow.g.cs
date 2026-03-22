using System;

namespace MarchDataMigration.Generated.Product
{
    // generated: source-side 1:1 shape
    public sealed class SourceProductRow
    {
        public Guid Id { get; set; }
        public Guid ProductCategoryId { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public string Status { get; set; }
        public decimal UnitCost { get; set; }
        public int UnitTypeId { get; set; }
        public string Description { get; set; }
        public string DetailsHTML { get; set; }
        public string RemoteResourceId { get; set; }
        public bool IsTrialResource { get; set; }
        public string icon { get; set; }
        public string ThumbnailImageResourceId { get; set; }
        public string ThumbnailImageResourceName { get; set; }
        public string ImageResourceId { get; set; }
        public string ImageResourceName { get; set; }
        public bool PhysicalProduct { get; set; }
        public string ShortSummaryHTML { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsPublic { get; set; }
        public int RecurringCycleTypeId { get; set; }
    }
}
