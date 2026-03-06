using LagoVista.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational.DataContexts
{

    [Table("SerialNumbers" , Schema = "dbo")]
    public class SerialNumberDTO
    {
        public int Index { get; set; }
        public string OrgId { get; set; }
        public string Key { get; set; }
        public string KeyId { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            var mb = modelBuilder;
            var provider = mb.GetProviderName();
            var entity = mb.Entity<SerialNumberDTO>();


            entity.HasKey(a => new { a.OrgId, a.Key, a.KeyId });

            entity.Property(x => x.Index).HasColumnOrder(1);
            entity.Property(x => x.OrgId).HasColumnOrder(2);
            entity.Property(x => x.Key).HasColumnOrder(3);
            entity.Property(x => x.KeyId).HasColumnOrder(4);

            entity.Property(x => x.Index).HasColumnType(StandardDBTypes.IntStorage(provider));
            entity.Property(x => x.Key).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.KeyId).HasColumnType(StandardDBTypes.TextShort(provider));
            entity.Property(x => x.OrgId).HasColumnType(StandardDBTypes.NormalizedId32Storage(provider));
        }
    }
}