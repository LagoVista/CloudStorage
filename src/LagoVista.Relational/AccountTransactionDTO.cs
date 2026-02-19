using LagoVista.Core;
using LagoVista.IoT.Billing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class AccountTransactionDto
    {
        [Key]
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string EncryptedAmount { get; set; }
        public bool IsReconciled { get; set; }
        public Guid TransactionCategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tag { get; set; }
        public string OriginalHash { get; set; }

        [Required]
        public string CreatedById { get; set; }

        [Required]
        public string LastUpdatedById { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

        [Required]
        public DateTime LastUpdateDate { get; set; }

        public Guid? VendorId { get; set; }

        [NotMapped]
        public AppUserDTO CreatedByUser { get; set; }

        [NotMapped]
        public AppUserDTO LastUpdatedByUser { get; set; }

        [NotMapped]
        public VendorDTO Vendor { get; set; }


        public AccountDto Account { get; set; }
        public AccountTransactionCategoryDto Category { get; set; }
    }
}
