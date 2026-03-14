using LagoVista.Models;
using Relational.Tests.Core.Utils;
using System;
using System.Collections.Generic;

namespace Relational.Tests.Core.Seeds
{
    public static class OrganizationSeeds
    {
        public const string PrimaryOrgId = "387CE1FCA7934C0DAC1BC85273C0AD0D";

        public static OrganizationDTO Primary { get; private set; }
        public static OrganizationDTO Secondary { get; private set; }

        public static OrganizationDTO SystemOwner { get; private set; }

        public static void Populate(int count)
        {
            All.Clear();

            if (count < 4) throw new ArgumentException("Count must be at least 4 to populate all seed data");
            var timeStamp = DateTime.UtcNow;

            for (var idx = 0; idx < count; ++idx)
            {
                var org = new OrganizationDTO
                {
                    OrgId = idx == 0 ? PrimaryOrgId : TestSeeds.CreateGuidString((byte)(idx + 1)),
                    OrgName = $"Organization Number {idx + 1}",
                    Status = "Active",
                    CreationDate = timeStamp.AddMinutes(-idx),
                    LastUpdatedDate = timeStamp.AddMinutes(-idx),
                    OrgBillingContactId = UserSeeds.All[idx].AppUserId,
                };

                if (idx == 0) Primary = org;
                if (idx == 1) Secondary = org;
                if (idx == 2) SystemOwner = org;
                All.Add(org);
            }
        }

        public static readonly List<OrganizationDTO> All = new List<OrganizationDTO>();
  
       
    }
}
