using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class LicenseUsageDTO
    {
        public Guid Id { get; set; }
        public Guid LicenseId { get; set; }
        public LicenseDTO Licene { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal QtyAdded { get; set; }
        public decimal QtyRemoved { get; set; }
        public string DeviceUniqueId { get; set; }
        public string DeviceId { get; set; }
        public string Notes { get; set; }
    }
}
