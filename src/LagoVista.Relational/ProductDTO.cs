using LagoVista.Core.Models;
using LagoVista.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    public class ProductDTO : DbModelBase
    {
     

        //public List<ProductPage> ProductPage { get; set; }

        public ProductCategoryDTO Category { get; set; }

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

        [NotMapped]
        public EntityHeader ImageResource
        {
            get
            {
                if (string.IsNullOrEmpty(ImageResourceId) || string.IsNullOrEmpty(ImageResourceName))
                    return null;

                return EntityHeader.Create(ImageResourceId, ImageResourceName);
            }
            set
            {
                if (value == null)
                {
                    ImageResourceId = null;
                    ImageResourceName = null;
                }
                else
                {
                    ImageResourceId = value.Id;
                    ImageResourceName = value.Text;
                }
            }
        }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), Key, Name);
        }


        [NotMapped]
        public EntityHeader ThumbnailImageResource
        {
            get
            {
                if (string.IsNullOrEmpty(ThumbnailImageResourceId) || string.IsNullOrEmpty(ThumbnailImageResourceName))
                    return null;

                return EntityHeader.Create(ThumbnailImageResourceId, ThumbnailImageResourceName);
            }
            set
            {
                if (value == null)
                {
                    ThumbnailImageResourceId = null;
                    ThumbnailImageResourceName = null;
                }
                else
                {
                    ThumbnailImageResourceId = value.Id;
                    ThumbnailImageResourceName = value.Text;
                }
            }
        }

        public ProductCategoryDTO ProductCategory { get; set; }

        //public List<ProductIncluded> SubProducts { get; set; }
        //public List<ProductIncluded> Packages { get; set; }


        [Required]
        public Guid ProductCategoryId { get; set; }
     }
}
