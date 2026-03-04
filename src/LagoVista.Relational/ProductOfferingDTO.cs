using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class ProductOfferingViewDTO
    {
        public Guid Id { get; set; }
        public Guid ProductCategoryId { get; set; }
        public string OrgId { get; set; }

        public string Name { get; set; }
        public string SummaryHtml { get; set; }

        public string ThumbNailImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public string UnitType { get; set; }
        public string Key { get; set; }
        public string ProductCategoryKey { get; set; }
        public string Sku { get; set; }
        public string DetailsHTML { get; set; }
        public string Description { get; set; }
        public string RemoteResourceId { get; set; }
    }
}
