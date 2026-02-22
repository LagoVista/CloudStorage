using LagoVista.Core.Attributes;
using LagoVista.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [EncryptionKey("Account-{id}", IdProperty = nameof(AccountId), CreateIfMissing = false)]
    public class AccountTransactionDto
    {
        [Key]
        public Guid Id { get; set; }


        [MapFrom("CreatedBy")]
        [Required]
        public string CreatedById { get; set; }

        [MapFrom("LastUpdatedBy")]
        [Required]
        public string LastUpdatedById { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

        [MapFrom("LastUpdatedDate")]
        [Required]
        public DateTime LastUpdateDate { get; set; }


        [MapTo("CreatedBy")]
        [NotMapped]
        public AppUserDTO CreatedByUser { get; set; }

        [MapTo("LastUpdatedBy")]
        [NotMapped]
        public AppUserDTO LastUpdatedByUser { get; set; }


        [Required]
        public Guid AccountId { get; set; }

        public Guid? VendorId { get; set; }

        [Required]
        public Guid TransactionCategoryId { get; set; }

        public DateOnly TransactionDate { get; set; }
        
        
        public string EncryptedAmount { get; set; }
        public bool IsReconciled { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tag { get; set; }
        public string OriginalHash { get; set; }




        [IgnoreOnMapTo]
        [NotMapped]
        public VendorDTO Vendor { get; set; }


        [IgnoreOnMapTo]
        public AccountDto Account { get; set; }

        [IgnoreOnMapTo]
        public AccountTransactionCategoryDto Category { get; set; }
    }
}
