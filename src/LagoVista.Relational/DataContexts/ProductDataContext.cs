using LagoVista.Core.Product;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{
    public class ProductDataContext : DbContext
    {
        public ProductDataContext(DbContextOptions<ProductDataContext> optionsContext) :
            base(optionsContext)
        {
        }

        public DbSet<ProductDTO> Product { get; set; }
        public DbSet<AppUserDTO> AppUser { get; set; }

        public DbSet<ProductCategoryDTO> ProductCategory { get; set; }
        public DbSet<ProductIncludedDTO> ProductIncluded { get; set; }
        public DbSet<ProductPageDTO> ProductPage { get; set; }
        public DbSet<ProductPageProductDTO> ProductPageProduct { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ProductCategoryDTO.Configure(modelBuilder);
            ProductDTO.Configure(modelBuilder); 
            ProductIncludedDTO.Configure(modelBuilder);
            ProductPageDTO.Configure(modelBuilder);
            ProductPageProductDTO.Configure(modelBuilder);


            modelBuilder.Entity<ProductOffering>(b =>
            {
                b.HasNoKey();
                b.ToView("usv_ProductOfferings"); // schema overload if needed: ("usv_ProductOfferings", "dbo")
            });
            
            modelBuilder.LowerCaseNames(Database.ProviderName);
        }
    }
}
