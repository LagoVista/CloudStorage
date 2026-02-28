using LagoVista.Models;
using LagoVista.Relational;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relational.Tests
{
    public class UserAdminSchemaTests : SchemaContractTestBase
    {
        protected override DbContext CreateContextForTruthDb(string sqlServerConnectionString)
        {
            var opts = new DbContextOptionsBuilder<UserAdminDataContext>()
                .UseSqlServer(sqlServerConnectionString)
                .EnableSensitiveDataLogging()
                .Options;

            return new UserAdminDataContext(opts);
        }


        [Test]
        public Task AppUsers() => AssertTableMatchesModelAsync(typeof(AppUserDTO), "dbo", "AppUser");
        [Test]
        public Task Organizations() => AssertTableMatchesModelAsync(typeof(OrganizationDTO), "dbo", "Org");

        [Test]
        public Task Subscriptions() => AssertTableMatchesModelAsync(typeof(SubscriptionDTO), "dbo", "Subscription");


        [Test]
        public Task DeviceOwners() => AssertTableMatchesModelAsync(typeof(DeviceOwnerDTO), "dbo", "DeviceOwnerUser");
        [Test]
        public Task DeviceOwnersDevices() => AssertTableMatchesModelAsync(typeof(OwnedDeviceDTO), "dbo", "DeviceOwnerUserDevices");


    }
}
