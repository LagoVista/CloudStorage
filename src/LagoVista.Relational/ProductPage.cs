using LagoVista.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    public class ProductPageDTO : DbModelBase
    {
        [Required]
        public string Key { get; set; }

        [Required]
        public string Name { get; set; }

        public bool IsPublic { get; set; }

        public string Icon { get; set; } = "icon-pz-product-2";

        [NotMapped]
        public List<ProductPageProductDTO> Products { get; set; } = new List<ProductPageProductDTO>();

        public string OrgId { get; set; }


        public string ShortSummaryHTML { get; set; }


        public string PageTitle { get; set; }


        public string HeroTitle { get; set; }
        public string HeroTagLine1 { get; set; }
        public string HeroTagLine2 { get; set; }



        public string DescriptionHtml { get; set; }

        public string VideoUrl { get; set; }

        public string HeroImageResourceId { get; set; }

        public string HeroImageResourceName { get; set; }

        public string ThumbnailImageResourceId { get; set; }
        public string ThumbnailImageResourceName { get; set; }

        public string ImageResourceId { get; set; }
        public string ImageResourceName { get; set; }
        public string TopLeftMenuId { get; set; }
        public string TopLeftMenuName { get; set; }

        public string TopRightMenuId { get; set; }
        public string TopRightMenuName { get; set; }


        public string ColorPaletteId { get; set; }
        public string ColorPaletteName { get; set; }

        public string ProductPageLayoutId { get; set; }
        public string ProductPageLayoutName { get; set; }

        public string BottomMenuId { get; set; }
        public string BottomMenuName { get; set; }

   
    }
}
