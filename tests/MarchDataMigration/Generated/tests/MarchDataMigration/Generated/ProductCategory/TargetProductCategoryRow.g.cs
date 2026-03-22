using System;

namespace MarchDataMigration.Generated.ProductCategory
{
    // generated: target-side 1:1 shape
    public partial class TargetProductCategoryRow
    {
        public Guid Id { get; set; }
        public string OrganizationId { get; set; }
        public string CreatedById { get; set; }
        public string LastUpdatedById { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public string Icon { get; set; }
        public string ThumbnailImageResourceId { get; set; }
        public string ThumbnailImageResourceName { get; set; }
        public string ImageResourceId { get; set; }
        public string ImageResourceName { get; set; }
        public string ShortSummaryHTML { get; set; }
        public string CategoryTypeId { get; set; }
        public string CategoryTypeName { get; set; }
    }
}
