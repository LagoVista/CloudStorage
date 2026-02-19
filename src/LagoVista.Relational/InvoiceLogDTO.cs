// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: d3714c8aba32044de83feac0abf2e177ef3b7e5554b20667f9d117e422826b80
// IndexVersion: 2
// --- END CODE INDEX META ---
using System;
using System.ComponentModel.DataAnnotations;

namespace LagoVista.Relational
{
    public class InvoiceLogsDTO
    {
        [Key]
        public Guid Id { get; set; }

        public Guid InvoiceId { get; set; }

        public DateTime DateStamp { get; set; }

        public string EventId { get; set; }
        public string EventData { get; set; }
        public string Message { get; set; }
   
        public InvoiceDTO Invoice { get; set; }
    }
}