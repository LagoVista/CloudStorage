using MarchDataMigration.Generated.InvoiceLogs;
using System;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class InvoiceLogsMapper
    {
        public static TargetInvoiceLogsRow Map(SourceInvoiceLogsRow source)
        {
            return new TargetInvoiceLogsRow
            {
                Id = source.Id,
                InvoiceId = source.InvoiceId,
                CustomerId = Guid.Parse("CD6328E3-4230-40F5-BBF7-173586119819"),
                DateStamp = source.DateStamp,
                EventId = source.EventId,
                EventData = source.EventData,
                Message = source.Message,
                EncryptedAmount = default,
            };
        }
    }
}
