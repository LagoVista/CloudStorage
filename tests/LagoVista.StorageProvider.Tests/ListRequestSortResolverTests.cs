using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LagoVista.StorageProvider.Tests
{
    [TestClass]
    public class ListRequestSortResolverTests
    {
        private sealed class SortableListRequest : ListRequest
        {
            public string SortField { get; set; }
            public bool? SortDescending { get; set; }
        }

        private sealed class Row
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int SortOrder { get; set; }
            public object Complex { get; set; }
        }

        private static IQueryable<Row> Rows() => new[]
        {
            new Row { Id = "b", Name = "Bravo", SortOrder = 20 },
            new Row { Id = "a", Name = "Alpha", SortOrder = 10 },
            new Row { Id = "c", Name = "Charlie", SortOrder = 20 },
        }.AsQueryable();

        [TestMethod]
        public void AppliesCaseInsensitiveAscendingSortWithStableIdTieBreaker()
        {
            var request = new SortableListRequest { SortField = "sortorder", SortDescending = false };

            var sorted = ListRequestSortResolver.Apply(Rows(), request).ToList();

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, sorted.Select(row => row.Id).ToArray());
        }

        [TestMethod]
        public void AppliesDescendingSortWithStableIdTieBreaker()
        {
            var request = new SortableListRequest { SortField = "SortOrder", SortDescending = true };

            var sorted = ListRequestSortResolver.Apply(Rows(), request).ToList();

            CollectionAssert.AreEqual(new[] { "c", "b", "a" }, sorted.Select(row => row.Id).ToArray());
        }

        [TestMethod]
        public void LeavesQueryUntouchedWhenSortFieldIsAbsent()
        {
            var request = new SortableListRequest();

            var sorted = ListRequestSortResolver.Apply(Rows(), request).ToList();

            CollectionAssert.AreEqual(new[] { "b", "a", "c" }, sorted.Select(row => row.Id).ToArray());
        }

        [TestMethod]
        public void RejectsUnknownSortField()
        {
            var request = new SortableListRequest { SortField = "DoesNotExist" };

            Assert.ThrowsException<ArgumentException>(() => ListRequestSortResolver.Apply(Rows(), request).ToList());
        }

        [TestMethod]
        public void RejectsNonScalarSortField()
        {
            var request = new SortableListRequest { SortField = "Complex" };

            Assert.ThrowsException<ArgumentException>(() => ListRequestSortResolver.Apply(Rows(), request).ToList());
        }
    }
}
