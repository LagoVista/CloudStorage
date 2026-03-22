using MarchDataMigration.Generated.BillingEvents;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class BillingEventsMapper
    {
        public static TargetBillingEventsRow Map(SourceBillingEventsRow source)
        {
            return new TargetBillingEventsRow
            {
                Id = source.Id,
                ResourceId = source.ResourceId,
                ResourceName = source.ResourceName,
                SubscriptionId = source.SubscriptionId,
                ProductId = source.ProductId,
                StartTimestamp = source.StartTimeStamp,
                StartedByAppUserId = source.StartedByAppUserId,
                EndTimestamp = source.EndTimeStamp,
                EndedByAppUserId = source.EndedByAppUserId,
                HoursBilled = source.HoursBilled,
                UnitCost = source.UnitCost,
                DiscountPercent = source.DiscountPercent,
                Extended = source.Extended,
                UnitTypeId = source.UnitTypeId,
                Notes = source.Notes,
                Status = source.Status,
                UnitPrice = source.UnitPrice,
                BillingTimeZoneId = 22,

                RolloverAt = default,
                BillingDate = default,
                Tokens = default,
            };
        }
    }
}
