using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    public class ProductDTO : DbModelBase
    {
        [IgnoreOnMapTo]
        public ProductCategoryDTO ProductCategory { get; set; }

        [Required]
        public Guid ProductCategoryId { get; set; }


        [Required]
        public string Key { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Sku { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public decimal UnitCost { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public int RecurringCycleTypeId { get; set; } = 1;

        [Required]
        public int UnitTypeId { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string DetailsHTML { get; set; }

        [Required]
        public string ShortSummaryHTML { get; set; }


        public string RemoteResourceId { get; set; }
        public bool IsTrialResource { get; set; }

        public bool PhysicalProduct { get; set; }


        public bool IsPublic { get; set; } = true;


        public string Icon { get; set; } = "icon-pz-product-1";

        public string ThumbnailImageResourceId { get; set; }
        public string ThumbnailImageResourceName { get; set; }

        public string ImageResourceId { get; set; }
        public string ImageResourceName { get; set; }


        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), Key, Name);
        }



     }
}
