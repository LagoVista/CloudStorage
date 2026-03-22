using MarchDataMigration.Generated.WorkRoles;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class WorkRolesMapper
    {
        public static TargetWorkRolesRow Map(SourceWorkRolesRow source)
        {
            return new TargetWorkRolesRow
            {
                Id = source.Id,
                OrganizationId = source.OrganizationId,
                Key = source.Key,
                Name = source.Name,
                Icon = source.Icon,
                IsActive = source.IsActive,
                Description = source.Description,
                CreationDate = source.CreationDate,
                CreatedById = source.CreatedById,
                LastUpdatedDate = source.LastUpdateDate,
                LastUpdatedById = source.LastUpdatedById,
            };
        }
    }
}
