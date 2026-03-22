using MarchDataMigration.Generated.DeviceOwnerUserDevices;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class DeviceOwnerUserDevicesMapper
    {
        public static TargetDeviceOwnerUserDevicesRow Map(SourceDeviceOwnerUserDevicesRow source)
        {
            return new TargetDeviceOwnerUserDevicesRow
            {
                Id = source.Id,
                DeviceUniqueId = source.DeviceUniqueId,
                DeviceName = source.DeviceName,
                DeviceId = source.DeviceId,
                DeviceOwnerUserId = source.DeviceOwnerUserId,
                ProductId = source.ProductId,
                Discount = source.Discount,
            };
        }
    }
}
