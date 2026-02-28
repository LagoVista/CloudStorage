using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace LagoVista.Relational
{
    [Table("Customers", Schema = "dbo")]
    public class CustomerDTO : DbModelBase
    {
        [Required]
        public string CustomerName { get; set; }
        public string BillingContactName { get; set; }
        public string BillingContactEmail { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        [Required]
        public string Notes { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        public string Zip { get; set; }

        [IgnoreOnMapTo]
        public IEnumerable<InvoiceDTO> Invoices { get; set; }

        public EntityHeader ToEntityHeader()
        {
            return EntityHeader.Create(Id.ToString(), CustomerName);
        }


        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerDTO>()
             .HasOne(ps => ps.Organization)
             .WithMany()
             .HasForeignKey(ps => ps.OrganizationId);

            modelBuilder.Entity<CustomerDTO>()
            .HasOne(ps => ps.CreatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<CustomerDTO>()
            .HasOne(ps => ps.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(ps => ps.LastUpdatedById)
            .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<CustomerDTO>()
                .HasMany(c => c.Invoices)
                .WithOne(i => i.Customer)
                .HasForeignKey(i => i.CustomerId);

            modelBuilder.Entity<CustomerDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.OrganizationId).HasColumnOrder(2);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.CustomerName).HasColumnOrder(3);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.BillingContactName).HasColumnOrder(4);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.BillingContactEmail).HasColumnOrder(5);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.Address1).HasColumnOrder(6);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.Address2).HasColumnOrder(7);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.City).HasColumnOrder(8);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.State).HasColumnOrder(9);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.Zip).HasColumnOrder(10);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.Notes).HasColumnOrder(11);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.CreatedById).HasColumnOrder(12);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.LastUpdatedById).HasColumnOrder(13);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.CreationDate).HasColumnOrder(14);
            modelBuilder.Entity<CustomerDTO>().Property(x => x.LastUpdateDate).HasColumnOrder(15);

            modelBuilder.Entity<CustomerDTO>().HasKey(x => new { x.Id });

            modelBuilder.Entity<CustomerDTO>().Property(x => x.Address1).HasColumnType("varchar(1024)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.Address2).HasColumnType("varchar(1024)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.BillingContactEmail).HasColumnType("varchar(1024)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.BillingContactName).HasColumnType("varchar(1024)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.City).HasColumnType("varchar(1024)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.CreatedById).HasColumnType("varchar(32)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.CreationDate).HasColumnType("datetime");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.CustomerName).HasColumnType("varchar(1024)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.Id).HasColumnType("uniqueidentifier");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.LastUpdateDate).HasColumnType("datetime");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.LastUpdatedById).HasColumnType("varchar(32)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.Notes).HasColumnType("varchar(max)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.OrganizationId).HasColumnType("varchar(32)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.State).HasColumnType("varchar(1024)");
            modelBuilder.Entity<CustomerDTO>().Property(x => x.Zip).HasColumnType("varchar(30)");
        }
    }
}