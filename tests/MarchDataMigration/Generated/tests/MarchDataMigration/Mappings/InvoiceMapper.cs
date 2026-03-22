using MarchDataMigration.Generated.Invoice;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class InvoiceMapper
    {
        public static TargetInvoiceRow Map(SourceInvoiceRow source)
        {
            return new TargetInvoiceRow
            {
                Id = source.Id,
                IsMaster = source.IsMaster,
                MasterInvoiceId = source.MasterInvoiceId,
                HasChildren = source.HasChildren,
                InvoiceNumber = source.InvoiceNumber,
                SubscriptionId = source.SubscriptionId,
                CustomerId = source.CustomerId.Value,
                Notes = source.Notes,
                BillingStart = source.BillingStart,
                BillingEnd = source.BillingEnd,
                ClosedTransactionId = source.ClosedTransactionId,
                CreationTimestamp = source.CreationTimeStamp,
                FailedAttemptCount = source.FailedAttemptCount,
                AgreementId = source.AgreementId,
                TaxPercent = source.TaxPercent,
                ContactId = source.ContactId,
                AdditionalNotes = source.AdditionalNotes ?? string.Empty,
                IsLocked = source.IsLocked,
                DueDate = source.DueDate,
                PaidDate = source.PaidDate,
                Status = source.Status,

                OrganizationId = source.OrgId,
                ServicesStart = source.BillingStart,
                ServicesEnd = source.BillingEnd,
                
                EncryptedTotal = source.Total,
                EncryptedDiscount = source.Discount,
                EncryptedExtended = source.Extended,
                EncryptedTotalPaid = source.TotalPaid,

                StatusTimestamp = source.StatusDate,
                EncryptedShipping = source.Shipping,
                EncryptedTax = source.Tax,
                EncryptedSubtotal = source.Subtotal,
                ContactName = source.ContactId,

                InvoiceDate = source.CreationTimeStamp,
            };
        }
    }
}
