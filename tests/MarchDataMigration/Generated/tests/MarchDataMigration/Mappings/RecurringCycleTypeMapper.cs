using MarchDataMigration.Generated.RecurringCycleType;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class RecurringCycleTypeMapper
    {
        public static TargetRecurringCycleTypeRow Map(SourceRecurringCycleTypeRow source)
        {
            return new TargetRecurringCycleTypeRow
            {
                Id = source.Id,
                Key = source.Key,
                Name = source.Name,
            };
        }
    }
}
