using LagoVista.CloudStorage.Utils;
using LagoVista.Relational;
using LagoVista.Relational.DataContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relational.Tests
{
    public sealed class ProductSchemaTests : SchemaContractTestBase
    {
        protected override DbContext CreateContextForTruthDb(string sqlServerConnectionString)
        {
            var opts = new DbContextOptionsBuilder<ProductDataContext>()
                .UseSqlServer(sqlServerConnectionString)
                .EnableSensitiveDataLogging()
                .Options;

            return new ProductDataContext(opts);
        }

        [Test]
        public Task Products() => AssertTableMatchesModelAsync(typeof(ProductDTO), "dbo", "Product");

        [Test]
        public Task ProductIncluded() => AssertTableMatchesModelAsync(typeof(ProductIncludedDTO), "dbo", "ProductIncluded");


        [Test]
        public Task ProductCategories() => AssertTableMatchesModelAsync(typeof(ProductCategoryDTO), "dbo", "ProductCategory");

        [Test]
        public Task ProductPages() => AssertTableMatchesModelAsync(typeof(ProductPageDTO), "dbo", "ProductPage");

        [Test]
        public Task ProductPageProducts() => AssertTableMatchesModelAsync(typeof(ProductPageProductDTO), "dbo", "ProductPage_Product");

    }

}
