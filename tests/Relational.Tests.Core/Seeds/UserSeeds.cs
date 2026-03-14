using LagoVista.Models;
using Relational.Tests.Core.Utils;
using System;
using System.Collections.Generic;

namespace Relational.Tests.Core.Seeds
{
    public static class UserSeeds
    {
        public const string PrimaryUserId = "69BA13A511EE4970829C9A0CCFEFB0AE";

        public static AppUserDTO Primary { get; private set; }
        public static AppUserDTO Secondary { get; private set; }

        public static void Populate(int count)
        {
            All.Clear();

            if (count < 4) throw new ArgumentException("Count must be at least 4 to populate all seed data");
            var timeStamp = DateTime.UtcNow;    

            for (var idx = 0; idx < count; ++idx)
            {
                var user = new AppUserDTO
                {
                    AppUserId = idx == 0 ? PrimaryUserId :TestSeeds.CreateNormalizedId32String((byte)(idx + 1)),
                    FullName = $"User {idx + 1}",
                    Email = $"user{idx + 1}@local",
                    CreationDate = timeStamp.AddMinutes(-idx),
                    LastUpdatedDate = timeStamp.AddMinutes(-idx)
                };

                if (idx == 0) Primary = user;
                if (idx == 1) Secondary = user;

                All.Add(user);
            }
        }

        public static readonly List<AppUserDTO> All = new List<AppUserDTO>();
    }
}
