using LagoVista;
using LagoVista.Core.AutoMapper;
using LagoVista.Core.AutoMapper.Converters;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Interfaces.Crypto;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Relational.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Relational.Tests
{
    [CriticalCoverage]
    [TestFixture]
    public class KeysetPagingTests
    {
        private SimpleDataContext _ctx;
        private LagoVistaAutoMapper _mapper;
        private PrettyCommandInterceptor _interceptor;

        [SetUp]
        public async Task Setup()
        {
            var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");
            await conn.OpenAsync().ConfigureAwait(false);

            _interceptor = new PrettyCommandInterceptor { IsReady = false };

            var builder = new DbContextOptionsBuilder<SimpleDataContext>();
            builder.AddInterceptors(_interceptor);

            var options = builder.UseSqlite(conn);
            _ctx = new SimpleDataContext(options.Options);

            await _ctx.Database.EnsureCreatedAsync();
            await PopulateList();

            CreateAutoMapper();
            _interceptor.IsReady = true;
        }


        private async Task Assert_First_Two_Pages_Are_Correct<TSort>(
            Expression<Func<SimpleRecord, TSort>> sortKey,
            Func<IQueryable<SimpleRecord>, IQueryable<SimpleRecord>> expectedOrder)
            where TSort : IComparable<TSort>
        {
            var partitionSelector = sortKey.Compile();

            var firstRequest = ListRequest.Create(1, 50);
            var firstPage = await _ctx.Records
                .ApplyKeysetPaging(firstRequest, sortKey, x => x.Id)
                .ToListResponseAsync(firstRequest, x => Map(x), partitionSelector, x => x.Id);

            Assert.That(firstPage.Model.Count, Is.EqualTo(50));
            Assert.That(firstPage.HasMoreRecords, Is.True);
            Assert.That(firstPage.NextPartitionKey, Is.Not.Null.And.Not.Empty);
            Assert.That(firstPage.NextRowKey, Is.Not.Null.And.Not.Empty);

            var secondRequest = ListRequest.Create(1, 50);
            secondRequest.NextPartitionKey = firstPage.NextPartitionKey;
            secondRequest.NextRowKey = firstPage.NextRowKey;

            var secondPage = await _ctx.Records
                .ApplyKeysetPaging(secondRequest, sortKey, x => x.Id)
                .ToListResponseAsync(secondRequest, x => Map(x), partitionSelector, x => x.Id);

            Assert.That(secondPage.Model.Count, Is.EqualTo(50));

            var overlap = firstPage.Model.Select(x => x.Id)
                .Intersect(secondPage.Model.Select(x => x.Id))
                .ToList();

            Assert.That(overlap, Is.Empty);

            var expectedFirst100 = await expectedOrder(_ctx.Records)
                .Take(100)
                .ToListAsync();

            var actualFirst100 = firstPage.Model
                .Concat(secondPage.Model)
                .Select(x => x.Id)
                .ToList();

            Assert.That(actualFirst100, Is.EqualTo(expectedFirst100.Select(x => x.Id)));
        }


        [Test]
        public async Task Should_Page_By_Name_Then_Id()
        {
            await Assert_First_Two_Pages_Are_Correct(
                x => x.Name,
                q => q.OrderByDescending(x => x.Name).ThenByDescending(x => x.Id));
        }

        [Test]
        public async Task Should_Page_By_Timestamp_Then_Id()
        {
            await Assert_First_Two_Pages_Are_Correct(
                x => x.Timestamp,
                q => q.OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.Id));
        }

        [Test]
        public async Task Should_Page_By_IntValue_Then_Id()
        {
            await Assert_First_Two_Pages_Are_Correct(
                x => x.IntValue,
                q => q.OrderByDescending(x => x.IntValue).ThenByDescending(x => x.Id));
        }

        [Test]
        public async Task Should_Page_By_DecimalValue_Then_Id()
        {
            await Assert_First_Two_Pages_Are_Correct_InMemoryExpected(
                x => x.DecimalValue,
                q => q.OrderByDescending(x => x.DecimalValue).ThenByDescending(x => x.Id));
        }


        private async Task PopulateList()
        {
            var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-500);
            var startTimestamp = DateTime.UtcNow.Date.AddDays(-500);

            for (var idx = 0; idx < 500; ++idx)
            {
                var date = startDate.AddDays(idx);
                var timestamp = startTimestamp.AddMinutes(idx * 7);

                _ctx.Records.Add(new SimpleRecord
                {
                    Id = Guid.NewGuid(),
                    Date = date,
                    Timestamp = timestamp,
                    IntValue = idx % 25,
                    DecimalValue = (idx % 20) + ((idx % 7) / 10m),
                    Name = $"Name-{idx % 15:D2}"
                });
            }

            await _ctx.SaveChangesAsync();
        }

        protected virtual void CreateAutoMapper()
        {
            var registry = ConvertersRegistration.DefaultConverterRegistery;
            var atomicBuilder = new ReflectionAtomicPlanBuilder(registry);
            var keyProvider = new EncryptionKeyProvider(new Mock<ISecureStorage>().Object);
            var planner = new EncryptedMapperPlanner(registry);
            var encryptor = new Encryptor();

            var encryptedMapper = new EncryptedMapper(
                keyProvider,
                planner,
                encryptor,
                new Mock<IModernEncryption>().Object,
                new Mock<IModernKeyIdBuilder>().Object);

            _mapper = new LagoVistaAutoMapper(encryptedMapper, atomicBuilder, registry);
        }

        private Task<SimpleRecord> Map(SimpleRecord record)
        {
            return _mapper.CreateAsync<SimpleRecord, SimpleRecord>(
                record,
                EntityHeader.Create("123", "user"),
                EntityHeader.Create("456", "org"));
        }

        private async Task<ListResponse<SimpleRecord>> GetPage(ListRequest request)
        {
            return await _ctx.Records
                .ApplyKeysetPaging(
                    request,
                    x => x.Date,
                    x => x.Id)
                .ToListResponseAsync(
                    request,
                    x => Map(x),
                    x => x.Date,
                    x => x.Id);
        }

        [Test]
        public async Task Should_Have_All_500_Records()
        {
            var records = await _ctx.Records.ToListAsync();
            Assert.That(records.Count, Is.EqualTo(500));
        }

        [Test]
        public async Task Should_Grab_First_Page_Based_On_Date()
        {
            var listRequest = ListRequest.Create(1, 50);

            var page = await GetPage(listRequest);

            Assert.That(page.Model.Count, Is.EqualTo(50));
        }

        [Test]
        public async Task First_Page_Should_Be_In_Descending_Date_Then_Id_Order()
        {
            var listRequest = ListRequest.Create(1, 50);

            var page = await GetPage(listRequest);

            Assert.That(page.Model.Count, Is.EqualTo(50));

            var expected = await _ctx.Records
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Take(50)
                .ToListAsync();

            Assert.That(page.Model.Select(x => x.Id), Is.EqualTo(expected.Select(x => x.Id)));
        }

        [Test]
        public async Task Second_Page_Should_Start_After_First_Page_Cursor_With_No_Overlap()
        {
            var firstRequest = ListRequest.Create(1, 50);
            var firstPage = await GetPage(firstRequest);

            Assert.That(firstPage.Model.Count, Is.EqualTo(50));
            Assert.That(firstPage.HasMoreRecords, Is.True);
            Assert.That(firstPage.NextPartitionKey, Is.Not.Null.And.Not.Empty);
            Assert.That(firstPage.NextRowKey, Is.Not.Null.And.Not.Empty);

            var secondRequest = ListRequest.Create(1, 50);
            secondRequest.NextPartitionKey = firstPage.NextPartitionKey;
            secondRequest.NextRowKey = firstPage.NextRowKey;

            var secondPage = await GetPage(secondRequest);

            Assert.That(secondPage.Model.Count, Is.EqualTo(50));

            Console.WriteLine($"FIRST PAGE: Date {firstPage.Model.First().Date}");
            Console.WriteLine($"SECOND PAGE: Date {secondPage.Model.First().Date}");

            var overlap = firstPage.Model.Select(x => x.Id)
                .Intersect(secondPage.Model.Select(x => x.Id))
                .ToList();

            Assert.That(overlap, Is.Empty);

            var expectedFirst100 = await _ctx.Records
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Take(100)
                .ToListAsync();

            var actualFirst100 = firstPage.Model
                .Concat(secondPage.Model)
                .Select(x => x.Id)
                .ToList();

            Assert.That(actualFirst100, Is.EqualTo(expectedFirst100.Select(x => x.Id)));
        }
        private async Task  Assert_First_Two_Pages_Are_Correct_InMemoryExpected<TSort>(
            Expression<Func<SimpleRecord, TSort>> sortKey,
            Func<IEnumerable<SimpleRecord>, IOrderedEnumerable<SimpleRecord>> expectedOrder)
            where TSort : IComparable<TSort>
        {
            var partitionSelector = sortKey.Compile();

            var firstRequest = ListRequest.Create(1, 50);
            var firstPage = await _ctx.Records
                .ApplyKeysetPaging(firstRequest, sortKey, x => x.Id)
                .ToListResponseAsync(firstRequest, x => Map(x), partitionSelector, x => x.Id);

            Assert.That(firstPage.Model.Count, Is.EqualTo(50));
            Assert.That(firstPage.HasMoreRecords, Is.True);
            Assert.That(firstPage.NextPartitionKey, Is.Not.Null.And.Not.Empty);
            Assert.That(firstPage.NextRowKey, Is.Not.Null.And.Not.Empty);

            var secondRequest = ListRequest.Create(1, 50);
            secondRequest.NextPartitionKey = firstPage.NextPartitionKey;
            secondRequest.NextRowKey = firstPage.NextRowKey;

            var secondPage = await _ctx.Records
                .ApplyKeysetPaging(secondRequest, sortKey, x => x.Id)
                .ToListResponseAsync(secondRequest, x => Map(x), partitionSelector, x => x.Id);

            Assert.That(secondPage.Model.Count, Is.EqualTo(50));

            var overlap = firstPage.Model.Select(x => x.Id)
                .Intersect(secondPage.Model.Select(x => x.Id))
                .ToList();

            Assert.That(overlap, Is.Empty);

            var allRecords = await _ctx.Records.ToListAsync();
            var expectedFirst100 = expectedOrder(allRecords)
                .Take(100)
                .ToList();

            var actualFirst100 = firstPage.Model
                .Concat(secondPage.Model)
                .Select(x => x.Id)
                .ToList();

            Assert.That(actualFirst100, Is.EqualTo(expectedFirst100.Select(x => x.Id)));
        }

        [Test]
        public async Task Should_Page_Through_All_Records_Without_Duplicates_Or_Gaps()
        {
            var seen = new List<Guid>();
            string nextPartitionKey = null;
            string nextRowKey = null;
            var pageNumber = 0;

            while (true)
            {
                pageNumber++;

                var request = ListRequest.Create(pageNumber, 50);
                request.NextPartitionKey = nextPartitionKey;
                request.NextRowKey = nextRowKey;

                var page = await GetPage(request);

                seen.AddRange(page.Model.Select(x => x.Id));

                if (!page.HasMoreRecords)
                    break;

                nextPartitionKey = page.NextPartitionKey;
                nextRowKey = page.NextRowKey;

                Assert.That(nextPartitionKey, Is.Not.Null.And.Not.Empty);
                Assert.That(nextRowKey, Is.Not.Null.And.Not.Empty);
            }

            Assert.That(seen.Count, Is.EqualTo(500));
            Assert.That(seen.Distinct().Count(), Is.EqualTo(500));

            var expected = await _ctx.Records
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Select(x => x.Id)
                .ToListAsync();

            Assert.That(seen, Is.EqualTo(expected));
        }

        [Test]
        public async Task Last_Page_Should_Have_Remaining_Records_And_No_More_Flag()
        {
            string nextPartitionKey = null;
            string nextRowKey = null;
            ListResponse<SimpleRecord> lastPage = null;

            while (true)
            {
                var request = ListRequest.Create(1, 50);
                request.NextPartitionKey = nextPartitionKey;
                request.NextRowKey = nextRowKey;

                var page = await GetPage(request);
                lastPage = page;

                if (!page.HasMoreRecords)
                    break;

                nextPartitionKey = page.NextPartitionKey;
                nextRowKey = page.NextRowKey;
            }

            Assert.That(lastPage, Is.Not.Null);
            Assert.That(lastPage.HasMoreRecords, Is.False);
            Assert.That(lastPage.Model.Count, Is.EqualTo(50));
        }

        [Test]
        public async Task Last_Page_Should_Be_Short_When_Page_Size_Does_Not_Divide_Total()
        {
            string nextPartitionKey = null;
            string nextRowKey = null;
            ListResponse<SimpleRecord> lastPage = null;

            var loopIdx = 0;

            while (true)
            {
                var request = ListRequest.Create(1, 64);
                request.NextPartitionKey = nextPartitionKey;
                request.NextRowKey = nextRowKey;

                var page = await GetPage(request);
                lastPage = page;

                 if (!page.HasMoreRecords )
                    break;

                nextPartitionKey = page.NextPartitionKey;
                nextRowKey = page.NextRowKey;
            }

            Assert.That(lastPage, Is.Not.Null);
            Assert.That(lastPage.HasMoreRecords, Is.False);
            Assert.That(lastPage.Model.Count, Is.EqualTo(52)); // 500 % 64 = 52
        }

        [Test]
        public async Task Cursor_Should_Resume_Exactly_After_Last_Item_Of_Previous_Page()
        {
            var firstPage = await GetPage(ListRequest.Create(1, 25));
            Assert.That(firstPage.Model.Count, Is.EqualTo(25));
            Assert.That(firstPage.HasMoreRecords, Is.True);

            var request = ListRequest.Create(1, 25);
            request.NextPartitionKey = firstPage.NextPartitionKey;
            request.NextRowKey = firstPage.NextRowKey;

            var secondPage = await GetPage(request);

            var fullyOrdered = await _ctx.Records
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Take(50)
                .ToListAsync();

            Assert.Multiple(() =>
            {
                Assert.That(firstPage.Model.Select(x => x.Id), Is.EqualTo(fullyOrdered.Take(25).Select(x => x.Id)));
                Assert.That(secondPage.Model.Select(x => x.Id), Is.EqualTo(fullyOrdered.Skip(25).Take(25).Select(x => x.Id)));
            });
        }

      
    }
}