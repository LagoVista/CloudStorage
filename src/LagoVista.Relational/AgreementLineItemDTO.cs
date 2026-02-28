using LagoVista.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    [Table("AgreementLineItems", Schema = "dbo")]
    public class AgreementLineItemDTO
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public Guid AgreementId { get; set; }
     
        [Required]
        public Guid ProductId { get; set; }
        [Required]
        public string ProductName { get; set; }

        public DateTime? Start { get; set; }

        public DateTime? End { get; set; }

        public DateTime? NextBillingDate { get; set; }
        public DateTime? LastBilledDate { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Extended { get; set; }

        public decimal SubTotal { get; set; }

        public decimal Shipping { get; set; }

        public decimal Quantity { get; set; }

        public bool Taxable { get; set; }

        public int UnitTypeId { get; set; }

        public bool IsRecurring { get; set; }

        public int? RecurringCycleTypeId { get; set; }

        [IgnoreOnMapTo]
        public ProductDTO Product { get; set; }

        [IgnoreOnMapTo]
        public AgreementDTO Agreement { get; set; }

        [IgnoreOnMapTo]
        public UnitTypeDTO UnitType { get; set; }

        [IgnoreOnMapTo]
        public RecurringCycleTypeDTO RecurringCycleType { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AgreementLineItemDTO>()
                .HasOne(ps => ps.Agreement)
                .WithMany(a => a.LineItems)
                .HasForeignKey(ps => ps.AgreementId);

            modelBuilder.Entity<AgreementLineItemDTO>()
                .HasOne(ps => ps.Product)
                .WithMany()
                .HasForeignKey(ps => ps.ProductId);

            modelBuilder.Entity<AgreementLineItemDTO>()
                .HasOne(ps => ps.UnitType)
                .WithMany()
                .HasForeignKey(ps => ps.UnitTypeId);


            modelBuilder.Entity<AgreementLineItemDTO>()
                .HasOne(ps => ps.RecurringCycleType)
                .WithMany()
                .HasForeignKey(ps => ps.RecurringCycleTypeId);

            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.AgreementId).HasColumnOrder(2);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.ProductId).HasColumnOrder(3);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.ProductName).HasColumnOrder(4);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.Start).HasColumnOrder(5);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.End).HasColumnOrder(6);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.UnitPrice).HasColumnOrder(7);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.DiscountPercent).HasColumnOrder(8);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.Extended).HasColumnOrder(9);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.SubTotal).HasColumnOrder(10);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.Quantity).HasColumnOrder(11);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.UnitTypeId).HasColumnOrder(12);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.IsRecurring).HasColumnOrder(13);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.RecurringCycleTypeId).HasColumnOrder(14);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.NextBillingDate).HasColumnOrder(15);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.LastBilledDate).HasColumnOrder(16);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.Taxable).HasColumnOrder(17);
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.Shipping).HasColumnOrder(18);

            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.Id).HasDefaultValueSql("newid()");
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.Shipping).HasDefaultValueSql("0");
            modelBuilder.Entity<AgreementLineItemDTO>().Property(x => x.Taxable).HasDefaultValueSql("0");

            modelBuilder.Entity<AgreementLineItemDTO>().HasKey(x => new { x.Id });
        }
    }
}
