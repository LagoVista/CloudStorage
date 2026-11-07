using Azure.Data.Tables;
using LagoVista.StorageMigration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StorageMigration.Tests;

[TestClass]
public class HistoricalOperationalMigrationTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 10, 30, 14, 50, 0, TimeSpan.Zero);

    private static MigrationCatalog Catalog() =>
        new(Path.Combine(AppContext.BaseDirectory, "Definitions"));

    [TestMethod]
    public void ThreeOperationalDefinitionsValidateWithoutCloudConnections()
    {
        var catalog = Catalog();
        foreach (var key in new[]
        {
            "marketing-contact-dto",
            "marketing-contact-phone-index",
            "useradmin-sent-email-xref"
        })
        {
            var definition = catalog.LoadByKey(key);
            Assert.AreEqual("cassandra-operational", definition.Target.Type);
            Assert.IsFalse(MigrationCatalog.Validate(definition).Any());
            Assert.AreEqual(64, catalog.DefinitionSha256(definition).Length);
        }
    }

    [TestMethod]
    public void OptionalFilterDoesNotChangeExistingMigrationDefinitionHash()
    {
        // Legacy completed passes rely on the SHA of the previously serialized definition.
        // New optional fields must be omitted when absent.
        var catalog = Catalog();
        var legacy = catalog.LoadByKey("useradmin-access-log");
        var filtered = catalog.LoadByKey("marketing-contact-phone-index");
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        var legacyJson = System.Text.Json.JsonSerializer.Serialize(legacy, options);
        var filteredJson = System.Text.Json.JsonSerializer.Serialize(filtered, options);
        Assert.IsFalse(legacyJson.Contains("requiredNonEmptyField", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(filteredJson.Contains("requiredNonEmptyField", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ContactProjectionPreservesIdentityAndOptOuts()
    {
        var definition = Catalog().LoadByKey("marketing-contact-dto");
        var row = new TableEntity("org-a", "contact-1")
        {
            Timestamp = Timestamp,
            ["DateAdded"] = "2026-10-30T14:50:00Z",
            ["Email"] = "person@example.com",
            ["Phone"] = "7275550100",
            ["DoNotContact"] = true,
            ["DoNotEmail"] = true
        };
        var mapped = new AzureTableRecordMapper().Map(definition, "ContactDTO", row);
        Assert.AreEqual("org-a", mapped["organization_id"]);
        Assert.AreEqual("contact-1", mapped["id"]);
        Assert.AreEqual("person@example.com", mapped["email"]);
        Assert.AreEqual("7275550100", mapped["phone"]);
        Assert.AreEqual(true, mapped["do_not_contact"]);
        Assert.AreEqual(true, mapped["do_not_email"]);
        Assert.IsFalse(mapped.ContainsKey("time_bucket"));
    }

    [TestMethod]
    public void PhoneIndexNormalizesNumbersAndSkipsMissingPhones()
    {
        var definition = Catalog().LoadByKey("marketing-contact-phone-index");
        var row = new TableEntity("org-a", "contact-1")
        {
            Timestamp = Timestamp,
            ["DateAdded"] = "2026-10-30T14:50:00Z",
            ["Phone"] = "+1 (727) 555-0100"
        };
        Assert.IsTrue(AzureTableMigrationSource.MatchesRequiredField(definition, row));
        var mapped = new AzureTableRecordMapper().Map(definition, "ContactDTO", row);
        Assert.AreEqual("org-a:contact-1", mapped["id"]);
        Assert.AreEqual("17275550100", mapped["organization_id"]);
        Assert.AreEqual("org-a", mapped["contact_organization_id"]);
        Assert.AreEqual("contact-1", mapped["contact_id"]);
        Assert.IsFalse(mapped.ContainsKey("time_bucket"));

        row.Remove("Phone");
        Assert.IsFalse(AzureTableMigrationSource.MatchesRequiredField(definition, row));
        row["Phone"] = " ";
        Assert.IsFalse(AzureTableMigrationSource.MatchesRequiredField(definition, row));
    }

    [TestMethod]
    public void ExternalEmailCrossReferencePreservesOriginalRowKey()
    {
        var definition = Catalog().LoadByKey("useradmin-sent-email-xref");
        var row = new TableEntity("org-b", "sendgrid-message-1")
        {
            Timestamp = Timestamp,
            ["SentEmailRowKey"] = "sent-email-1"
        };
        var mapped = new AzureTableRecordMapper().Map(definition, "SentEmailXRefDTO", row);
        Assert.AreEqual("sendgrid-message-1", mapped["id"]);
        Assert.AreEqual("sendgrid-message-1", mapped["organization_id"]);
        Assert.AreEqual("org-b", mapped["sent_email_organization_id"]);
        Assert.AreEqual("sent-email-1", mapped["sent_email_row_key"]);
        Assert.IsFalse(mapped.ContainsKey("time_bucket"));
    }
}
