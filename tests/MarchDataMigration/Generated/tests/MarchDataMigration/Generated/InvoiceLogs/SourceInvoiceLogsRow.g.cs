using System;

namespace MarchDataMigration.Generated.InvoiceLogs
{
    // generated: source-side 1:1 shape
    public sealed class SourceInvoiceLogsRow
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public DateTime DateStamp { get; set; }
        public string EventId { get; set; }
        public string EventData { get; set; }
        public string Message { get; set; }
    }
}
