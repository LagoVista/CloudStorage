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

        public DbSet<ProductCategoryDTO> ProductCategory { get; set; }
        public DbSet<ProductDTO> Product { get; set; }
        public DbSet<AppUserDTO> AppUser { get; set; }
        public DbSet<ProductIncludedDTO> ProductIncluded { get; set; }
        public DbSet<ProductPageDTO> ProductPage { get; set; }
        public DbSet<ProductPageProductDTO> ProductPageProductDTO { get; set; }
        public DbSet<ProductPageProductDTO> ProductPageProduct { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<ProductDTO>()
                .HasOne(tp => tp.Category)
                .WithMany()
                .HasForeignKey(p => p.ProductCategoryId);

            modelBuilder.Entity<ProductDTO>()
                .HasOne(tp => tp.CreatedByUser)
                .WithMany()
                .HasForeignKey(tp => tp.CreatedById);

            modelBuilder.Entity<ProductDTO>()
                .HasOne(tp => tp.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(tp => tp.LastUpdatedById);

            modelBuilder.Entity<ProductPageDTO>()
                .HasOne(tp => tp.CreatedByUser)
                .WithMany()
                .HasForeignKey(tp => tp.CreatedById);

            modelBuilder.Entity<ProductPageDTO>()
                .HasOne(tp => tp.LastUpdatedByUser)
                .WithMany()
                .HasForeignKey(tp => tp.LastUpdatedById);

            modelBuilder.Entity<ProductPageDTO>()
                .HasMany<ProductPageProductDTO>()
                .WithOne()
                .HasForeignKey(ppp => ppp.ProductId);

            //modelBuilder.Entity<Product>()
            //    .HasMany(tp => tp.SubProducts)
            //    .WithOne()
            //    .HasForeignKey(tp => tp.Product);

            ////modelBuilder.Entity<ProductIncluded>()
            ////    .HasOne(tp => tp.Package)
            ////    .WithMany(tp => tp.Packages);

            //modelBuilder.Entity<ProductIncluded>()
            //    .HasOne(tp => tp.Product)
            //    .WithMany(tp => tp.SubProducts)
            //    .HasForeignKey(sp=>sp.ProductId);

            modelBuilder.LowerCaseNames();




            /*    modelBuilder.Entity<ProductPage>()
                       .HasMany(e => e.Products)
                       .WithMany(e => e.ProductPage)
                       .UsingEntity(
                           nameof(ProductPageProductDTO),
                           l => l.HasOne(typeof(Product)).WithMany().HasForeignKey(nameof(Models.ProductPageProductDTO.ProductId)).HasPrincipalKey(nameof(Models.Product.Id)),
                           r => r.HasOne(typeof(ProductPage)).WithMany().HasForeignKey(nameof(Models.ProductPageProductDTO.ProductPageId)).HasPrincipalKey(nameof(Models.ProductPage.Id)),
                           j => j.HasKey(nameof(Models.ProductPageProductDTO.ProductPageId), nameof(Models.ProductPageProductDTO.ProductId)));*/


            /* TODO: Add model building stuff for migrations */
        }
    }
}
