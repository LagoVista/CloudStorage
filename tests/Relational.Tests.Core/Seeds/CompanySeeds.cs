using LagoVista.Models;
using Relational.Tests.Core.Utils;
using System;
using System.Collections.Generic;

namespace Relational.Tests.Core.Seeds
{
    public static class CompanySeeds
    {
        public static readonly DateTime SeedNowUtc = new DateTime(2026, 01, 15, 12, 00, 00, DateTimeKind.Utc);

        public static AppUserDTO Kevin = new AppUserDTO() { AppUserId = TestSeeds.CreateNormalizedId32String(0x80), FullName = "Kevin Leven", Email = "kevin.leven@example.com", CreationDate = SeedNowUtc, LastUpdatedDate = SeedNowUtc };
        public static AppUserDTO Frank = new AppUserDTO() { AppUserId = TestSeeds.CreateNormalizedId32String(0x81), FullName = "Frank Banks", Email = "frank.banks@example.com", CreationDate = SeedNowUtc, LastUpdatedDate = SeedNowUtc };
        public static AppUserDTO Bill = new AppUserDTO() { AppUserId = TestSeeds.CreateNormalizedId32String(0x82), FullName = "Bill Will", Email = "bill.will@example.com", CreationDate = SeedNowUtc, LastUpdatedDate = SeedNowUtc };
        public static AppUserDTO Sally = new AppUserDTO() { AppUserId = TestSeeds.CreateNormalizedId32String(0x83), FullName = "Sally Wally", Email = "sallywally@example.com", CreationDate = SeedNowUtc, LastUpdatedDate = SeedNowUtc };
        public static AppUserDTO Tracey = new AppUserDTO() { AppUserId = TestSeeds.CreateNormalizedId32String(0x84), FullName = "Tracey Marcey", Email = "traceymarcey@example.com", CreationDate = SeedNowUtc, LastUpdatedDate = SeedNowUtc };

        public static List<AppUserDTO> AllUsers = new() { Kevin, Frank, Bill, Sally, Tracey };

    }
}