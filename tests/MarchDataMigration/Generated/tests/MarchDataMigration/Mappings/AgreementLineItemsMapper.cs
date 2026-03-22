using MarchDataMigration.Generated.AgreementLineItems;
using System;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class AgreementLineItemsMapper
    {
        public static TargetAgreementLineItemsRow Map(SourceAgreementLineItemsRow source)
        {
            return new TargetAgreementLineItemsRow
            {
                Id = source.Id,
                AgreementId = source.AgreementId,
                ProductId = source.ProductId,
                CustomerId = Guid.Parse("CD6328E3-4230-40F5-BBF7-173586119819"),
                ProductName = source.ProductName,
                Start = source.Start,
                End = source.End,
                EncryptedUnitPrice = source.UnitPrice.ToString(),
                EncryptedDiscountPercent = source.DiscountPercent.ToString(),
                EncryptedExtended = source.Extended.ToString(),
                EncryptedSubTotal = source.SubTotal.ToString(),
                Quantity = source.Quantity,
                UnitTypeId = source.UnitTypeId,
                IsRecurring = source.IsRecurring,
                RecurringCycleTypeId = source.RecurringCycleTypeId,
                NextBillingDate = source.NextBillingDate,
                LastBilledDate = source.LastBilledDate,
                Taxable = source.Taxable,
                EncryptedShipping = source.Shipping.ToString(),
            };
        }
    }
}
