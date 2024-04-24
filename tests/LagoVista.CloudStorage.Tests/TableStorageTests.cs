using Azure.Data.Tables;
using LagoVista.CloudStorage.Tests;
using LagoVista.CloudStorage.Tests.Support;
using LagoVista.Core;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.IntegrationTests
{
    public class TableStorageTests
    {
        private string _accountId;
        private string _accountKey;

        TSEntityRepo _entityRepo;

        const string PARITION_KEY = "01763EBDE58E41C1B69D395AD1A03A21";

        [SetUp]
        public void Setup()
        {
            _accountId = Environment.GetEnvironmentVariable("TEST_AZURESTORAGE_ACCOUNTID");
            _accountKey = Environment.GetEnvironmentVariable("TEST_AZURESTORAGE_ACCESSKEY");

            if (String.IsNullOrEmpty(_accountId)) throw new ArgumentNullException("Please add TEST_AZURESTORAGE_ACCOUNTID as an environnment variable");
            if (String.IsNullOrEmpty(_accountKey)) throw new ArgumentNullException("Please add TEST_AZURESTORAGE_ACCESSKEY as an environnment variable");

            _entityRepo = new TSEntityRepo(_accountId, _accountKey, new AdminLogger(new ConsoleLogWriter()));
        }

        private async Task InsertManyItems(int count)
        {
            for (var idx = 0; idx < count; idx++)
            {
                var entity = new TSEntity()
                {
                    RowKey = DateTime.UtcNow.ToInverseTicksRowKey(),
                    PartitionKey = PARITION_KEY,
                    Value1 = "NAME_VAL_1",
                    Value2 = "NAME_VAL_2",
                    Index = idx,
                };

                await _entityRepo!.AddTSEtnityAsync(entity);
            }
        }

        [Test]
        public async Task Insert200Items()
        {
            await InsertManyItems(200);
        }

        [Test]
        public async Task Should_Insert_TSEntity()
        {
            var entity = new TSEntity()
            {
                RowKey = DateTime.UtcNow.ToInverseTicksRowKey(),
                PartitionKey = PARITION_KEY,
                Value1 = "NAME_VAL_1",
                Value2 = "NAME_VAL_2",
            };

            await _entityRepo!.AddTSEtnityAsync(entity);
        }

        [Test]
        public async Task Should_Remove_All_By_PartitionKey()
        {
            await InsertManyItems(50);
            await _entityRepo!.RemoveByPartitionKeyAsync(PARITION_KEY);
        }

        [Test]
        public async Task Should_Delete_Entity()
        {
            var entity = new TSEntity()
            {
                RowKey = DateTime.UtcNow.ToInverseTicksRowKey(),
                PartitionKey = PARITION_KEY,
                Value1 = "NAME_VAL_1",
                Value2 = "NAME_VAL_2",
            };

            await _entityRepo!.AddTSEtnityAsync(entity);
            await _entityRepo!.RemoveAsync(entity.PartitionKey, entity.RowKey);
        }

        [Test]
        public async Task Should_Insert_And_Read_TSEntity()
        {
            var rowKey = DateTime.UtcNow.ToInverseTicksRowKey();

            var entity = new TSEntity()
            {
                RowKey = rowKey,
                PartitionKey = PARITION_KEY,
                Value1 = "NAME_VAL_1" + DateTime.Now.ToJSONString(),
                Value2 = "NAME_VAL_2" + DateTime.Now.ToJSONString(),
            };

            await _entityRepo!.AddTSEtnityAsync(entity);

            var existing = await _entityRepo.GetAsync(PARITION_KEY, rowKey);

            Assert.AreEqual(entity.RowKey, existing.RowKey);

            Assert.AreEqual(entity.Value1, existing.Value1);
            Assert.AreEqual(entity.Value2, existing.Value2);
        }


        [TearDown]
        public async Task CleanUp()
        {
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_accountId};AccountKey={_accountKey}";

            var tableClient = new TableClient(connectionString, nameof(TSEntity));
            await tableClient.DeleteAsync();
        }

    }
}
