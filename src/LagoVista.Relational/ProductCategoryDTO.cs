using LagoVista.Core.Models;
using LagoVista.Models;
using System;
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

        private EntityHeader _categoryType;
        [NotMapped]
        public EntityHeader CategoryType
        {
            get
            {
                if (_categoryType == null)
                {
                    if (!String.IsNullOrEmpty(CategoryTypeId) && !String.IsNullOrEmpty(CategoryTypeName))
                    {
                        _categoryType = EntityHeader.Create(CategoryTypeId, CategoryTypeName);
                    }
                    else
                    {
                        return null;
                    }
                }

                Console.WriteLine($"{CategoryTypeName} - {CategoryTypeId}");

                return _categoryType;
            }
            set
            {
                _categoryType = value;
                if (value != null)
                {
                    CategoryTypeName = value.Text;
                    CategoryTypeId = value.Key;
                }
                else
                {
                    CategoryTypeName = null;
                    CategoryTypeId = null;
                }
            }
        }

        public string CategoryTypeName { get; set; }

        public string CategoryTypeId { get; set; }


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


    }
}
