using MarchDataMigration.Generated.UnitType;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class UnitTypeMapper
    {
        public static TargetUnitTypeRow Map(SourceUnitTypeRow source)
        {
            return new TargetUnitTypeRow
            {
                Id = source.Id,
                Name = source.Name,
                Key = source.Key,
            };
        }
    }
}
