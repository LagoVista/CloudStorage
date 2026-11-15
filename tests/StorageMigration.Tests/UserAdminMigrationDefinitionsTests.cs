using Azure.Data.Tables;
using LagoVista.StorageMigration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using System.Security.Cryptography;
using System.Text;

namespace StorageMigration.Tests;

[TestClass]
public class UserAdminMigrationDefinitionsTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
    private static MigrationCatalog Catalog() => new(Path.Combine(AppContext.BaseDirectory, "Definitions"));

    [DataTestMethod]
    [DataRow("useradmin-app-instance", "mongo-application", "AppInstanceRecord")]
    [DataRow("useradmin-org-membership", "mongo-application", "OrgMembershipRecord")]
    [DataRow("useradmin-user-role", "cassandra-operational", "user_role_operational_record")]
    [DataRow("useradmin-role-access", "cassandra-operational", "role_access_operational_record")]
    public void Four_UserAdmin_Definitions_Validate_Offline(string key, string type, string table)
    {
        var definition = Catalog().LoadByKey(key);
        Assert.AreEqual(type, definition.Target.Type);
        Assert.AreEqual(table, definition.Target.Table);
        Assert.AreEqual("user-storage", definition.Source.Connection);
        Assert.IsFalse(MigrationCatalog.Validate(definition).Any());
        Assert.AreEqual(64, Catalog().DefinitionSha256(definition).Length);
    }

    [TestMethod]
    public void AppInstance_Identity_Matches_Production_Repository_Id_Formula()
    {
        var row = new TableEntity("USER-1", "INSTANCE-1")
        {
            Timestamp = Timestamp,
            ["UserId"] = "USER-1",
            ["AppId"] = "app-one",
            ["ClientType"] = "desktop",
            ["LastLogin"] = "2026-11-15T10:00:00Z"
        };
        var definition = Catalog().LoadByKey("useradmin-app-instance");
        var mapped = new AzureTableRecordMapper().Map(definition, "AppInstance", row);
        var input = "useradmin-appinstance:v1:" + "USER-1".Length + ":" + "USER-1" + ":" + "INSTANCE-1";
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)).AsSpan(0, 16));
        Assert.AreEqual(expected, mapped["Id"]);
        Assert.AreEqual("USER-1", mapped["UserPartition"]);
        Assert.AreEqual("INSTANCE-1", mapped["InstanceKey"]);
        Assert.AreEqual("app-one", mapped["AppId"]);

        var writer = new MongoApplicationDataMigrationWriter(nullDatabase(), "SYSTEM-ORG", "System");
        var doc = writer.BuildDocument(definition, mapped);
        Assert.AreEqual(expected, doc["_id"].AsString);
        Assert.AreEqual("SYSTEM-ORG", doc["Organization"]["Id"].AsString);
        Assert.AreEqual("System", doc["Organization"]["Text"].AsString);
        Assert.AreEqual("app-one", doc["AppId"].AsString);
        Assert.AreEqual("USER-1", doc["UserPartition"].AsString);
        Assert.IsTrue(doc.Contains("_storageVersion"));
        Assert.IsTrue(doc["CreationDate"].IsString);
        Assert.IsTrue(doc["LastUpdatedDate"].IsString);
        Assert.IsFalse(doc.Contains("Id"));
    }

    [TestMethod]
    public void Membership_Maps_Original_Permissions_And_NonDeleted_State()
    {
        var row = new TableEntity("ORG-1", "ORG-1.USER-1")
        {
            Timestamp = Timestamp,
            ["OrgId"] = "ORG-1",
            ["UserId"] = "USER-1",
            ["IsOrgAdmin"] = true,
            ["IsAppBuilder"] = false,
            ["DefaultRoleId"] = "role-1",
            ["Email"] = " Ada@Example.COM "
        };
        var definition = Catalog().LoadByKey("useradmin-org-membership");
        var mapped = new AzureTableRecordMapper().Map(definition, "OrgUser", row);
        var expectedInput = "useradmin-orgmembership:v1:" + "ORG-1".Length + ":" + "ORG-1" + ":" + "USER-1";
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(expectedInput)).AsSpan(0, 16)), mapped["Id"]);
        Assert.AreEqual("ORG-1.USER-1", mapped["RowKey"]);
        Assert.AreEqual(false, mapped["IsDeleted"]);
        Assert.AreEqual(true, mapped["IsOrgAdmin"]);
        Assert.AreEqual(false, mapped["IsAppBuilder"]);
        Assert.AreEqual("ADA@EXAMPLE.COM", mapped["EmailKey"]);
    }

    [TestMethod]
    public void UserRole_Definition_Preserves_Id_And_CAS_Initial_Version()
    {
        var row = new TableEntity("ORG-1", "ASSIGNMENT-1")
        {
            Timestamp = Timestamp,
            ["UserId"] = "USER-1",
            ["RoleId"] = "ROLE-1",
            ["RoleName"] = "Editor",
            ["OrganizationId"] = "ORG-1"
        };
        var definition = Catalog().LoadByKey("useradmin-user-role");
        var mapped = new AzureTableRecordMapper().Map(definition, "UserRoleDTO", row);
        Assert.AreEqual("ASSIGNMENT-1", mapped["id"]);
        Assert.AreEqual("ORG-1", mapped["organization_id"]);
        Assert.AreEqual("USER-1", mapped["user_id"]);
        Assert.AreEqual("ROLE-1", mapped["role_id"]);
        Assert.AreEqual(1L, mapped["version"]);
        Assert.AreEqual(false, mapped["is_deleted"]);
    }

    [TestMethod]
    public void Public_Role_Permissions_Use_Global_Partition()
    {
        var definition = Catalog().LoadByKey("useradmin-role-access");
        var publicRow = new TableEntity("ORG-1", "ACCESS-1")
        {
            Timestamp = Timestamp,
            ["OrganizationId"] = "ORG-1",
            ["RoleId"] = "ROLE-1",
            ["IsPublic"] = true,
            ["ModuleId"] = "MODULE-1",
            ["Create"] = 2,
            ["Read"] = 1,
            ["Update"] = 0,
            ["Delete"] = 0
        };
        var mapped = new AzureTableRecordMapper().Map(definition, "RoleAccessDTO", publicRow);
        Assert.AreEqual("public_role_access", mapped["organization_id"]);
        Assert.AreEqual("ORG-1", mapped["source_organization_id"]);
        Assert.AreEqual("ACCESS-1", mapped["id"]);
        Assert.AreEqual(2, mapped["create"]);
        Assert.AreEqual(1L, mapped["version"]);
        publicRow["IsPublic"] = false;
        var privateMapped = new AzureTableRecordMapper().Map(definition, "RoleAccessDTO", publicRow);
        Assert.AreEqual("ORG-1", privateMapped["organization_id"]);
    }

    [TestMethod]
    public void Mongo_Definition_Requires_Explicit_Owner_Scope()
    {
        var def = Catalog().LoadByKey("useradmin-app-instance");
        def.Target.OwnerOrganizationIdVariable = null;
        Assert.IsTrue(MigrationCatalog.Validate(def).Any(e => e.Contains("ownerOrganizationIdVariable")));
    }

    private static MongoDB.Driver.IMongoDatabase nullDatabase()
        => new MongoDB.Driver.MongoClient("mongodb://localhost:27017").GetDatabase("offline_test_only");
}
