using MarchDataMigration.Generated.Agreements;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class AgreementsMapper
    {
        public static TargetAgreementsRow Map(SourceAgreementsRow source)
        {
            return new TargetAgreementsRow
            {
                Id = source.Id,
                CustomerId = source.CustomerId,
                OrganizationId = source.OrganizationId,
                Name = source.Name,
                Identifier = source.Identifier,
                Locked = source.Locked,
                Internal = source.Internal,
                InvoicePeriod = source.InvoicePeriod,
                Terms = source.Terms,
                Start = source.Start,
                End = source.End,
                Status = source.Status,
                Hours = source.Hours,
                EncryptedRate = source.EncryptedRate,
                Notes = source.Notes,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = default,
                LastInvoicedDate = source.LastInvoicedDate,
                NextInvoiceDate = source.NextInvoiceDate,
                CustomerContactId = source.CustomerContactId,
                CustomerContactName = source.CustomerContactName,
                EncryptedSubTotal = source.SubTotal.ToString(),
                EncryptedDiscountPercent = source.DiscountPercent.ToString(),
                EncryptedTax = source.Tax.ToString(),
                EncryptedShipping = source.Shipping.ToString(),
                EncryptedTotal = source.Total.ToString(),
                TaxPercent = source.TaxPercent,
            };
        }
    }
}
