using LagoVista.CloudStorage.Storage;
using LagoVista.Relational.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace LagoVista.StorageProvider.Tests.Postgres
{
    [TestClass]
    public class PostgresMetricsStoreUnitTests
    {
        [TestMethod]
        public void PercentileAggregates_MapToTimescaleCompatibleOrderedSetExpressions()
        {
            var method = typeof(PostgresMetricsStore).GetMethod("AggregateExpression", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);

            Assert.AreEqual("percentile_cont(0.50) WITHIN GROUP (ORDER BY value)", method.Invoke(null, new object[] { MetricAggregate.Percentile50 }));
            Assert.AreEqual("percentile_cont(0.95) WITHIN GROUP (ORDER BY value)", method.Invoke(null, new object[] { MetricAggregate.Percentile95 }));
        }
    }
}
