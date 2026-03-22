using MarchDataMigration.Generated.DeviceOwnerUser;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class DeviceOwnerUserMapper
    {
        public static TargetDeviceOwnerUserRow Map(SourceDeviceOwnerUserRow source)
        {
            return new TargetDeviceOwnerUserRow
            {
                DeviceOwnerUserId = source.DeviceOwnerUserId,
                Email = source.Email,
                Phone = source.Phone,
                FullName = source.FullName,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdatedDate,
            };
        }
    }
}
