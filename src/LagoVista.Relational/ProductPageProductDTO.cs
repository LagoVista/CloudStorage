using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("ProductPage_Product")]
    public class ProductPageProductDTO
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid ProductPageId { get; set; }
        public decimal Discount { get; set; }
        public int Index { get; set; }
        public int UnitQty { get; set; }
    }
}
