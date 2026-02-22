using LagoVista.Core.Attributes;
using LagoVista.Models;
using Microsoft.Identity.Client;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [EncryptionKey("BUDGET_ITEM_KEY_{id}", IdProperty = nameof(OrganizationId), CreateIfMissing = false)]
    public class BudgetItemDTO : DbModelBase
    {
        
        public int Year { get; set; }

        public int Month { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }

        public Guid? AccountTransactionCategoryId { get; set; }

        public Guid? ExpenseCategoryId { get; set; }

        public Guid? WorkRoleId { get; set; }

        [NotMapped]
        public AccountTransactionCategoryDto AccountTransactionCategory { get; set; }

        [NotMapped]
        public ExpenseCategoryDTO ExpenseCategory { get; set; }

        [NotMapped]
        public WorkRoleDTO WorkRole { get; set; }


        public string EncryptedAllocated { get; set; }
        public string EncryptedActual { get; set; }

    }
}
