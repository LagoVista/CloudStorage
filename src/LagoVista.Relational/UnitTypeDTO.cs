using LagoVista.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("UnitType", Schema = "dbo")]
    public class UnitTypeDTO
    {
        [Key]
        public int Id { get; set; }


        [Required]
        public string Name { get; set; }

        [Required]
        public string Key { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnitTypeDTO>().Property(x => x.Id).HasColumnOrder(1);
            modelBuilder.Entity<UnitTypeDTO>().Property(x => x.Name).HasColumnOrder(2);
            modelBuilder.Entity<UnitTypeDTO>().Property(x => x.Key).HasColumnOrder(3);

            modelBuilder.Entity<UnitTypeDTO>().HasKey(x => new { x.Id });

            modelBuilder.Entity<UnitTypeDTO>().Property(x => x.Id).HasColumnType("int");
            modelBuilder.Entity<UnitTypeDTO>().Property(x => x.Key).HasColumnType("varchar(max)");
            modelBuilder.Entity<UnitTypeDTO>().Property(x => x.Name).HasColumnType("varchar(max)");
        }

        public EntityHeader ToEntityHeader()
        {
            return new EntityHeader()
            {
                Id = Id.ToString(),
                Key = Key,
                Text = Name,
            };
        }
    }
}
