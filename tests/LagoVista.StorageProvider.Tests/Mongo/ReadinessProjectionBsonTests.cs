using LagoVista.CloudStorage.Models.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [TestCategory("Mongo")]
    public sealed class ReadinessProjectionBsonTests
    {
        [TestInitialize]
        public void Initialize()
        {
            MongoBsonSerialization.Configure();
        }

        [TestMethod]
        public void ScorecardProjection_DeserializesReadinessChecksArray()
        {
            var document = new BsonDocument
            {
                { "_id", "ENTITY-1" },
                { "EntityType", "SOP" },
                { "ReadinessChecks", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "CheckType", 0 },
                            { "Status", 1 },
                            { "RequiredStepCount", 2 },
                            { "CompletedStepCount", 2 },
                            { "BlockingReasons", new BsonArray() }
                        }
                    }
                }
            };

            var projection = BsonSerializer.Deserialize<ReadinessScorecardProjection>(document);

            Assert.AreEqual("ENTITY-1", projection.Id);
            Assert.IsInstanceOfType(projection.ReadinessChecks, typeof(JArray));
            Assert.AreEqual(1, projection.ReadinessChecks[0]["Status"].Value<int>());
            Assert.AreEqual(2, projection.ReadinessChecks[0]["CompletedStepCount"].Value<int>());
        }

        [TestMethod]
        public void ScorecardProjection_DeserializesQualitySummaryWithoutFullAssessment()
        {
            var document = new BsonDocument
            {
                { "_id", "ENTITY-GRADED" },
                { "EntityType", "ArtifactSpecification" },
                { "ReadinessChecks", new BsonArray() },
                { "MasterStatus", new BsonDocument
                    {
                        { "QualityScore", 87.5 },
                        { "QualityGrade", "B+" },
                        { "GradingSpecificationVersion", "1" },
                        { "GradingStatus", "Graded" },
                        { "GradingSourceFingerprint", "sha256-example" }
                    }
                }
            };
            var projection = BsonSerializer.Deserialize<ReadinessScorecardProjection>(document);
            Assert.IsInstanceOfType(projection.MasterStatus, typeof(JObject));
            Assert.AreEqual(87.5, projection.MasterStatus["QualityScore"].Value<double>(), 0.001);
            Assert.AreEqual("B+", projection.MasterStatus["QualityGrade"].Value<string>());
            Assert.AreEqual("Graded", projection.MasterStatus["GradingStatus"].Value<string>());
        }

        [TestMethod]
        public void CandidateProjection_DeserializesAllJsonFields()
        {
            var document = new BsonDocument
            {
                { "_id", "ENTITY-2" },
                { "EntityType", "SOP" },
                { "ChecklistStatus", new BsonArray { new BsonDocument { { "StepKey", "draft" } } } },
                { "ReadinessChecks", new BsonArray { new BsonDocument { { "CheckType", 0 }, { "Status", 2 } } } },
                { "MasterStatus", new BsonDocument { { "TotalReadinessCheckCount", 1 }, { "IsProductionReady", false } } }
            };

            var projection = BsonSerializer.Deserialize<ReadinessCandidateProjection>(document);

            Assert.IsInstanceOfType(projection.ChecklistStatus, typeof(JArray));
            Assert.IsInstanceOfType(projection.ReadinessChecks, typeof(JArray));
            Assert.IsInstanceOfType(projection.MasterStatus, typeof(JObject));
            Assert.AreEqual("draft", projection.ChecklistStatus[0]["StepKey"].Value<string>());
            Assert.AreEqual(2, projection.ReadinessChecks[0]["Status"].Value<int>());
            Assert.AreEqual(1, projection.MasterStatus["TotalReadinessCheckCount"].Value<int>());
            Assert.IsFalse(projection.MasterStatus["IsProductionReady"].Value<bool>());
        }

        [TestMethod]
        public void ScorecardProjection_DocumentShapedReadinessChecksDoesNotThrow()
        {
            // Existing records may contain a BSON document rather than an array.
            // The projection must preserve the value instead of failing to load.
            var document = new BsonDocument
            {
                { "_id", "ENTITY-3" },
                { "EntityType", "SOP" },
                { "ReadinessChecks", new BsonDocument { { "State", "legacy" } } }
            };

            var projection = BsonSerializer.Deserialize<ReadinessScorecardProjection>(document);

            Assert.IsInstanceOfType(projection.ReadinessChecks, typeof(JObject));
            Assert.AreEqual("legacy", projection.ReadinessChecks["State"].Value<string>());
        }

        [TestMethod]
        public void JTokenSerializer_RoundTripsScalarsAndNulls()
        {
            var samples = new JToken[]
            {
                new JValue("text"),
                new JValue(42),
                new JValue(true),
                JValue.CreateNull(),
                new JArray(1, 2, 3),
                new JObject { ["name"] = "example", ["count"] = 4 }
            };

            foreach (var sample in samples)
            {
                var serialized = new TokenEnvelope { Value = sample }.ToBsonDocument();
                var result = BsonSerializer.Deserialize<TokenEnvelope>(serialized);

                Assert.IsTrue(JToken.DeepEquals(sample, result.Value),
                    "JToken round-trip failed for " + sample.Type);
            }
        }

        private sealed class TokenEnvelope
        {
            public JToken Value { get; set; }
        }
    }
}
