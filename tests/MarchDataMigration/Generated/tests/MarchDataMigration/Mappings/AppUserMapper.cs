using MarchDataMigration.Generated.AppUser;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class AppUserMapper
    {
        public static TargetAppUserRow Map(SourceAppUserRow source)
        {
            return new TargetAppUserRow
            {
                AppUserId = source.AppUserId,
                Email = source.Email,
                FullName = source.FullName,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdatedDate,
            };
        }
    }
}
