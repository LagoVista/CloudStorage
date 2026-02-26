using LagoVista.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class ProductIncludedDTO
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid PackageId { get; set; }
        [Required]
        public Guid ProductId { get; set; }
        [Required]
        public decimal Discount { get; set; }
        public string Notes { get; set; }

        public int Quantity { get; set; }

        public string Name { get; set; }
        public string Key { get; set; }

        [IgnoreOnMapTo()]
        public ProductDTO Package { get; set; }

        [IgnoreOnMapTo()]
        public ProductDTO Product { get; set; }
    }
}
