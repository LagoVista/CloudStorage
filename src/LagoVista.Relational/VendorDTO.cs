using LagoVista.Core.Attributes;
using LagoVista.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    public class VendorDTO : DbModelBase
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Key { get; set; }

        public string Contact { get; set; }
        public string Phone { get; set; }

        public string Description { get; set; }

        public string Notes { get; set; }

        public string Icon { get; set; }

        public Guid DefaultExpenseCategoryId { get; set; }
        public Guid? DefaultAccountTransactionCategoryId { get; set; }

        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string StateOrProvince { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }

        public string PayPeriod { get; set; }
        public decimal MaxAmount { get; set; }

        public bool IsActive { get; set; }

        [IgnoreOnMapTo()]
        [NotMapped]
        public ExpenseCategoryDTO DefaultExpenseCategory { get; set; }


        [IgnoreOnMapTo()]
        [NotMapped]
        public AccountTransactionCategoryDto DefaultAccountTransactionCategory { get; set; }

    }
}
