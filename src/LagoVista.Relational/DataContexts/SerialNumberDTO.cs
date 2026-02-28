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
            modelBuilder.Entity<SerialNumberDTO>().Property(x => x.Index).HasColumnOrder(1);
            modelBuilder.Entity<SerialNumberDTO>().Property(x => x.OrgId).HasColumnOrder(2);
            modelBuilder.Entity<SerialNumberDTO>().Property(x => x.Key).HasColumnOrder(3);
            modelBuilder.Entity<SerialNumberDTO>().Property(x => x.KeyId).HasColumnOrder(4);

        }
    }
}