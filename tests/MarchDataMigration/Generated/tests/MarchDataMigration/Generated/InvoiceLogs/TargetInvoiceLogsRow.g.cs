using System;

namespace MarchDataMigration.Generated.InvoiceLogs
{
    // generated: target-side 1:1 shape
    public partial class TargetInvoiceLogsRow
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime DateStamp { get; set; }
        public string EventId { get; set; }
        public string EventData { get; set; }
        public string Message { get; set; }
        public string EncryptedAmount { get; set; }
    }
}
