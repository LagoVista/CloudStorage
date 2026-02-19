using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class OwnedDeviceDTO
    {
        [Key]
        public string Id { get; set; }
        public string DeviceUniqueId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceOwnerUserId { get; set; }
        public Guid ProductId { get; set; }
        public decimal Discount { get; set; }
    }
}
