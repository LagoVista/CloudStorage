using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class AccountTransactionCategoryDto : DbModelBase
    {
        public Guid? ExpenseCategoryId { get; set; }


        public ExpenseCategoryDTO ExpenseCategory { get; set; }


        public string Type { get; set; }
        public string OrganizationId { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool IsActive { get; set; }

        public string TaxCategory { get; set; }

        public bool TaxReportable { get; set; }

        public bool Passthrough { get; set; }

        public EntityHeader ToEntityHeader()
        {
            return new EntityHeader()
            {
                Id = Id.ToString(),
                Text = Name,
            };
        }
    }
}
