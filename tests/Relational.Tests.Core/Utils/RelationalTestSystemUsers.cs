using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Relational.Tests.Core.Seeds;

namespace LagoVista.Relational.Tests.Core.Utils
{
    public class RelationalTestSystemUsers : ISystemUsers 
    {
        public EntityHeader SystemOrg => OrganizationSeeds.Primary.ToEntityHeader();

        public EntityHeader InstanceUser => UserSeeds.Primary.ToEntityHeader();

        public EntityHeader DeviceManagerUser => UserSeeds.Primary.ToEntityHeader();

        public EntityHeader JobServiceUser => UserSeeds.Primary.ToEntityHeader();

        public EntityHeader HostUser => UserSeeds.Primary.ToEntityHeader();
    }
}
