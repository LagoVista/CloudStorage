using LagoVista.Core.Models;

namespace Relational.Tests.Core.Seeds
{
    public static class TestSeedIdentity
    {
        public const string OrgId = OrganizationSeeds.PrimaryOrgId;
        public const string UserId = UserSeeds.PrimaryUserId;
        public static readonly EntityHeader OrgEH = OrganizationSeeds.Primary.ToEntityHeader();
        public static readonly EntityHeader UserEH = UserSeeds.Primary.ToEntityHeader();
    }
}
