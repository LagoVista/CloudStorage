using LagoVista.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

    public class ProductPageProductViewDTO
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid ProductPageId { get; set; }

        public decimal Discount { get; set; }
        public string Name { get; set; }
        public string SKU { get; set; }
        public string ShortSummaryHTML { get; set; }
        public string DetailsHTML { get; set; }
        public string Description { get; set; }
        public string ImageResourceId { get; set; }
        public string ThumbnailImageResourceId { get; set; }
        public string RemoteResourceId { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitPrice { get; set; }
        public string UnitType { get; set; }
        public int Index { get; set; }
        public int UnitQty { get; set; }
        public int Units { get; set; }
        public decimal Extended { get; set; }
        public int UnitTypeId { get; set; }
        public Guid ProductCategoryId { get; set; }
    }
}
