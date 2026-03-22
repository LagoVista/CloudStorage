using System;

namespace MarchDataMigration.Generated.WorkRoles
{
    // generated: target-side 1:1 shape
    public partial class TargetWorkRolesRow
    {
        public Guid Id { get; set; }
        public string OrganizationId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public DateTime CreationDate { get; set; }
        public string CreatedById { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string LastUpdatedById { get; set; }
    }
}
