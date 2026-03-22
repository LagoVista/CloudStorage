using MarchDataMigration.Generated.InvoiceLineItems;
using System;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class InvoiceLineItemsMapper
    {
        public static TargetInvoiceLineItemsRow Map(SourceInvoiceLineItemsRow source)
        {
            return new TargetInvoiceLineItemsRow
            {
                Id = source.Id,
                InvoiceId = source.InvoiceId,
                CustomerId = Guid.Parse("CD6328E3-4230-40F5-BBF7-173586119819"),
                AgreementId = source.AgreementId,
                ResourceId = source.ResourceId,
                ResourceName = source.ResourceName,
                ProductName = source.ProductName,
                Quantity = source.Quantity,
                Units = source.Units,
                EncryptedUnitPrice = source.UnitPrice,
                EncryptedTotal = source.Total,
                EncryptedDiscount = source.Discount,
                EncryptedExtended = source.Extended,
                Taxable = source.Taxable,
                ProductId = source.ProductId,
                EncryptedShipping = source.Shipping,
            };
        }
    }
}
