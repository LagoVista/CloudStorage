using LagoVista.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("ExpenseCategory")]
    public class ExpenseCategoryDTO : DbModelBase
    {
        public ExpenseCategoryDTO()
        {
            Icon = "icon-fo-grow-dollar";
        }

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
