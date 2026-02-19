using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class DeviceOwnerDTO
    {
        [Key]
        public string DeviceOwnerUserId { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
    }
}
