using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    public class ProductCategoryDTO : DbModelBase
    {

        public ProductCategoryDTO()
        {
            Icon = "icon-pz-product-2";
            IsPublic = true;
            CategoryTypeId = "-1";
            CategoryTypeName = "-select category type-";
        }

        public string Key { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsPublic { get; set; }

        public string OrgId { get; set; }
        public string Icon { get; set; }


        public string ShortSummaryHTML { get; set; }


        public string CategoryTypeName { get; set; }

        public string CategoryTypeId { get; set; }


        public string ThumbnailImageResourceId { get; set; }
        public string ThumbnailImageResourceName { get; set; }

        public string ImageResourceId { get; set; }
        public string ImageResourceName { get; set; }
        public EntityHeader ToEntityHeader() => EntityHeader.Create(this.Id.ToString(), this.Key, this.Name);
        

        [IgnoreOnMapTo()]
        public List<ProductDTO> Products { get; set; }
        
    }
}
