using LagoVista.Core;
using LagoVista.Core.Models;
using Relational.Tests.Core.Seeds;
using System;


namespace Relational.Tests.Core.Utils
{
    public static class TestObjectStamping
    {
        public static T StampRelational<T>(
            this T entity,
            EntityHeader org = null,
            EntityHeader user = null,
            UtcTimestamp? nowUtc = null) where T : RelationalEntityBase
        {
            var now = nowUtc ?? UtcTimestamp.Now;
            
            entity.OwnerOrganization = org ?? OrganizationSeeds.Primary.ToEntityHeader();
            entity.CreatedBy = user ?? UserSeeds.Primary.ToEntityHeader();
            entity.LastUpdatedBy = user ?? UserSeeds.Primary.ToEntityHeader();
            entity.CreationDate = now;
            entity.LastUpdatedDate = now;

            return entity;
        }
    }

    public static class TestObjectBuilderExtensions
    {
        public static T With<T>(this T obj, Action<T> mutate)
        {
            mutate?.Invoke(obj);
            return obj;
        }
    }
}
