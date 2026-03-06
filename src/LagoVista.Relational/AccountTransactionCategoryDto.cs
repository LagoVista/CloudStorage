using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("AccountTransactionCategory", Schema = "dbo")]
    public class AccountTransactionCategoryDto : DbModelBase, IEntityHeaderFactory
    {
        public Guid? ExpenseCategoryId { get; set; }




        [Required]
        public string Type { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Icon { get; set; }
        public bool IsActive { get; set; }

        public string TaxCategory { get; set; }

        public bool TaxReportable { get; set; }

        public bool Passthrough { get; set; }

        public ExpenseCategoryDTO ExpenseCategory { get; set; }
        public EntityHeader ToEntityHeader()
        {
            return new EntityHeader()
            {
                Id = Id.ToString(),
                Text = Name,
            };
        }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<AccountTransactionCategoryDto>();

            // Relationships
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedById);
            entity.HasOne(x => x.LastUpdatedByUser).WithMany().HasForeignKey(x => x.LastUpdatedById).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.SetNull);

            // Key / indexes / concurrency
            entity.HasKey(x => x.Id);

            // Defaults
            entity.Property(x => x.Icon).HasDefaultValueSql(StandardDbDefaults.Text(provider, "icon-ae-checklist-2"));
            entity.Property(x => x.IsActive).HasDefaultValueSql(StandardDbDefaults.True(provider));
            entity.Property(x => x.Passthrough).HasDefaultValueSql(StandardDbDefaults.False(provider));
            entity.Property(x => x.TaxReportable).HasDefaultValueSql(StandardDbDefaults.False(provider));

            // Column order
            entity.Property(x => x.Id).HasColumnOrder(1);
            entity.Property(x => x.OrganizationId).HasColumnOrder(2);
            entity.Property(x => x.Name).HasColumnOrder(3);
            entity.Property(x => x.Type).HasColumnOrder(4);
            entity.Property(x => x.Description).HasColumnOrder(5);
            entity.Property(x => x.CreatedById).HasColumnOrder(6);
            entity.Property(x => x.LastUpdatedById).HasColumnOrder(7);
            entity.Property(x => x.CreationDate).HasColumnOrder(8);
            entity.Property(x => x.LastUpdateDate).HasColumnOrder(9);
            entity.Property(x => x.IsActive).HasColumnOrder(10);
            entity.Property(x => x.Icon).HasColumnOrder(11);
            entity.Property(x => x.ExpenseCategoryId).HasColumnOrder(12);
            entity.Property(x => x.TaxCategory).HasColumnOrder(13);
            entity.Property(x => x.TaxReportable).HasColumnOrder(14);
            entity.Property(x => x.Passthrough).HasColumnOrder(15);

            // Storage types
            entity.Property(x => x.Id).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.OrganizationId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.Name).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.Type).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.Description).HasColumnType(StandardDBTypes.TextMedium(provider));
            entity.Property(x => x.CreatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.LastUpdatedById).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
            entity.Property(x => x.CreationDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.LastUpdateDate).HasColumnType(StandardDBTypes.UtcTimestampStorage(provider));
            entity.Property(x => x.IsActive).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.Icon).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.ExpenseCategoryId).HasColumnType(StandardDBTypes.UuidStorage(provider));
            entity.Property(x => x.TaxCategory).HasColumnType(StandardDBTypes.TextTiny(provider));
            entity.Property(x => x.TaxReportable).HasColumnType(StandardDBTypes.FlagStorage(provider));
            entity.Property(x => x.Passthrough).HasColumnType(StandardDBTypes.FlagStorage(provider));
        }
    }
}
