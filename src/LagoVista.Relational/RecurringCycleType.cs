using LagoVista.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("RecurringCycleType", Schema = "dbo")]
    public class RecurringCycleTypeDTO
    {
        [Key]
        public int Id { get; set; }


        [Required]
        public string Name { get; set; }

        [Required]
        public string Key { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecurringCycleTypeDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<RecurringCycleTypeDTO>().Property(x => x.Key).HasColumnOrder(2);
            modelBuilder.Entity<RecurringCycleTypeDTO>().Property(x => x.Name).HasColumnOrder(3);

            modelBuilder.Entity<RecurringCycleTypeDTO>().HasKey(x => new { x.Id });

            modelBuilder.Entity<RecurringCycleTypeDTO>().Property(x => x.Id).HasColumnType("int");
            modelBuilder.Entity<RecurringCycleTypeDTO>().Property(x => x.Key).HasColumnType("varchar(1024)");
            modelBuilder.Entity<RecurringCycleTypeDTO>().Property(x => x.Name).HasColumnType("varchar(1024)");
        }

        public EntityHeader ToEntityHeader()
        {
            return new EntityHeader()
            {
                Id = Id.ToString(),
                Key  = Key,
                Text = Name,
            };
        }   
    }
}
