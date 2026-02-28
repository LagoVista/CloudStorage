using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("Expenses", Schema = "dbo")]
    public class ExpenseDTO : DbModelBase
    {
        public Guid? AgreementId { get; set; }
        [Required]
        public Guid TimePeriodId { get; set; }
        public Guid? BillingEventId { get; set; }

        public Guid? VendorId { get; set; }

        public Guid? PaymentId { get; set; }

        [Obsolete("use ExpenseDate")]
        public DateTime Date { get; set; }

        [Required]
        public DateOnly ExpenseDate { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public Guid ExpenseCategoryId { get; set; }
        public string Notes { get; set; }
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string WorkTaskId { get; set; }
        public string WorkTaskName { get; set; }

        public bool Approved { get; set; }
        public string ApprovedById { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public VendorDTO Vendor { get; set; }

        public AgreementDTO Agreement { get; set; }
        public ExpenseCategoryDTO Category { get; set; }
        public PaymentDTO Payment { get; set; }
        public TimePeriodDTO TimePeriod { get; set; }

        public AppUserDTO User { get; set; }
        public AppUserDTO ApprovedUser { get; set; }

        public bool Locked { get; set; }

        [Required]
        public string Description { get; set; }
        [Required]
        public string EncryptedAmount { get; set; }
        [Required]
        public string EncryptedReimbursedAmount { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Agreement)
            .WithMany()
            .HasForeignKey(x => x.AgreementId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Category)
            .WithMany()
            .HasForeignKey(x => x.ExpenseCategoryId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Vendor)
            .WithMany()
            .HasForeignKey(ex => ex.VendorId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.TimePeriod)
            .WithMany()
            .HasForeignKey(x => x.TimePeriodId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.ApprovedUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedById);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(x => x.LastUpdatedById);

            modelBuilder.Entity<ExpenseDTO>()
            .HasOne(ex => ex.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId);

            modelBuilder.Entity<ExpenseDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.TimePeriodId).HasColumnOrder(2);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.ExpenseCategoryId).HasColumnOrder(3);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.AgreementId).HasColumnOrder(4);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.BillingEventId).HasColumnOrder(5);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.PaymentId).HasColumnOrder(6);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.Date).HasColumnOrder(7);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.ExpenseDate).HasColumnOrder(8);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.ProjectId).HasColumnOrder(9);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.ProjectName).HasColumnOrder(10);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.WorkTaskId).HasColumnOrder(11);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.WorkTaskName).HasColumnOrder(12);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.UserId).HasColumnOrder(13);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.OrganizationId).HasColumnOrder(14);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.Approved).HasColumnOrder(15);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.ApprovedById).HasColumnOrder(16);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.ApprovedDate).HasColumnOrder(17);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.Locked).HasColumnOrder(18);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.EncryptedAmount).HasColumnOrder(19);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.EncryptedReimbursedAmount).HasColumnOrder(20);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.Notes).HasColumnOrder(21);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.Description).HasColumnOrder(22);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.CreatedById).HasColumnOrder(23);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(24);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.CreationDate).HasColumnOrder(25);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(26);
            modelBuilder.Entity<ExpenseDTO>().Property(x => x.VendorId).HasColumnOrder(27);

            modelBuilder.Entity<ExpenseDTO>().HasKey(x => new { x.Id });
        }
    }
}
