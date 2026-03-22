using LagoVista.Core;
using MarchDataMigration.Generated.Subscription;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class SubscriptionMapper
    {
        public static TargetSubscriptionRow Map(SourceSubscriptionRow source)
        {
            var startDate = source.CreationDate.ToDateOnly().ToDateTime();
            var trialEnd = startDate.AddDays(360).ToDateOnly().ToDateTime();

            var subscription = new TargetSubscriptionRow
            {
                Id = source.Id,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdatedDate,
                OrganizationId = source.OrgId,
                Name = source.Name,
                Key = source.Key,
                Status = source.Status,
                Description = source.Description,
                Icon = source.Icon,
                Start = startDate,
                CustomerId = default,
                PaymentTokenStatus = "ok",

                PaymentTokenCustomerId = default,
                PaymentTokenSecretId = default,
                PaymentTokenDate = default,
                PaymentTokenExpires = default,
                End = default,
                PaymentAccountId = default,
                PaymentAccountType = default,
                IsActive = true,
                ActiveDate = startDate,
                IsTrial = source.Key == "trial",
            };

            if(subscription.IsTrial)
            {
                var expired = trialEnd < DateTime.UtcNow;
                subscription.IsActive = !expired;
                subscription.PaymentTokenStatus = "waived";
                subscription.TrialStartDate = startDate;
                subscription.TrialExpirationDate = trialEnd;
                if(expired)
                {
                    subscription.InactiveDate = trialEnd;
                    subscription.Status = "expired";
                    subscription.PaymentTokenStatus = "expired";
                    subscription.End = trialEnd;
                }
            }

            return subscription;
        }
    }
}
