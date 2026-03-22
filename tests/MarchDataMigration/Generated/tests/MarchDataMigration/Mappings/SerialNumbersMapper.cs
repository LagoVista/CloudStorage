using MarchDataMigration.Generated.SerialNumbers;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class SerialNumbersMapper
    {
        public static TargetSerialNumbersRow Map(SourceSerialNumbersRow source)
        {
            return new TargetSerialNumbersRow
            {
                Index = source.Index,
                OrgId = source.OrgId,
                Key = source.Key,
                KeyId = source.KeyId,
            };
        }
    }
}
