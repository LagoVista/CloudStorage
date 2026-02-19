using LagoVista.Core;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    [Table("ExpenseCategory")]
    public class ExpenseCategoryDTO : DbModelBase
    {
        public ExpenseCategoryDTO()
        {
            Icon = "icon-fo-grow-dollar";
        }

        public string OrganizationId { get; set; }

        [Required]
        public string Key { get; set; }
        public bool IsActive { get; set; }
        public bool RequiresApproval { get; set; }

        [Required]
        public string Icon { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal ReimbursementPercent { get; set; }
        public decimal DeductiblePercent { get; set; }

        public string TaxCategory { get; set; }
    }
}
