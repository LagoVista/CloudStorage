using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using LagoVista.Relational.DataContexts;

#nullable disable

namespace LagoVista.Relational.Migrations.SemanticPostgres
{
    /// <inheritdoc />
    [DbContext(typeof(SemanticPostgresMigrationContext))]
    [Migration("20261113120000_InitialSemanticPostgres")]
    public partial class InitialSemanticPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "artifacts",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(32)", nullable: false),
                    ownerorganizationid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    key = table.Column<string>(type: "varchar(64)", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    tla = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    description = table.Column<string>(type: "varchar(1024)", nullable: true),
                    purposesummary = table.Column<string>(type: "varchar(1024)", nullable: true),
                    statuskey = table.Column<string>(type: "varchar(50)", nullable: false),
                    scopetypekey = table.Column<string>(type: "varchar(64)", nullable: false),
                    archetypekey = table.Column<string>(type: "varchar(64)", nullable: false),
                    productioncardinalitykey = table.Column<string>(type: "varchar(64)", nullable: false),
                    specificationsha256 = table.Column<string>(type: "varchar(64)", nullable: false),
                    creationdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lastupdateddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artifacts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "concepts",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(32)", nullable: false),
                    ownerorganizationid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    key = table.Column<string>(type: "varchar(64)", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    description = table.Column<string>(type: "varchar(1024)", nullable: true),
                    aliases = table.Column<string>(type: "varchar(1024)", nullable: true),
                    statuskey = table.Column<string>(type: "varchar(50)", nullable: false),
                    creationdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lastupdateddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concepts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registry",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ownerorganizationid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    seededfromrevision = table.Column<long>(type: "bigint", nullable: true),
                    seededutc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdutc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedutc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(32)", nullable: false),
                    ownerorganizationid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    key = table.Column<string>(type: "varchar(64)", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    description = table.Column<string>(type: "varchar(1024)", nullable: true),
                    aliases = table.Column<string>(type: "varchar(1024)", nullable: true),
                    statuskey = table.Column<string>(type: "varchar(50)", nullable: false),
                    creationdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lastupdateddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "definitions",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(32)", nullable: false),
                    ownerorganizationid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    subjectid = table.Column<string>(type: "varchar(32)", nullable: false),
                    conceptid = table.Column<string>(type: "varchar(32)", nullable: false),
                    qualifiedkey = table.Column<string>(type: "varchar(128)", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    summary = table.Column<string>(type: "varchar(2048)", nullable: false),
                    example1 = table.Column<string>(type: "varchar(1024)", nullable: true),
                    example2 = table.Column<string>(type: "varchar(1024)", nullable: true),
                    example3 = table.Column<string>(type: "varchar(1024)", nullable: true),
                    statuskey = table.Column<string>(type: "varchar(50)", nullable: false),
                    definitionsha256 = table.Column<string>(type: "varchar(64)", nullable: false),
                    creationdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lastupdateddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_Definitions_Concepts_ConceptId",
                        column: x => x.conceptid,
                        principalSchema: "dbo",
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Definitions_Subjects_SubjectId",
                        column: x => x.subjectid,
                        principalSchema: "dbo",
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "artifactinformationelements",
                schema: "dbo",
                columns: table => new
                {
                    artifactid = table.Column<string>(type: "varchar(32)", nullable: false),
                    definitionid = table.Column<string>(type: "varchar(32)", nullable: false),
                    ownerorganizationid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    usagerole = table.Column<string>(type: "varchar(50)", nullable: true),
                    creationdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lastupdateddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactInformationElements", x => new { x.artifactid, x.definitionid });
                    table.ForeignKey(
                        name: "FK_ArtifactInformationElements_Artifacts_ArtifactId",
                        column: x => x.artifactid,
                        principalSchema: "dbo",
                        principalTable: "artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtifactInformationElements_Definitions_DefinitionId",
                        column: x => x.definitionid,
                        principalSchema: "dbo",
                        principalTable: "definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "embeddings",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ownerorganizationid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    definitionid = table.Column<string>(type: "varchar(32)", nullable: false),
                    modelkey = table.Column<string>(type: "varchar(128)", nullable: false),
                    dimensions = table.Column<int>(type: "integer", nullable: false),
                    sourcesha256 = table.Column<string>(type: "varchar(64)", nullable: false),
                    vector = table.Column<byte[]>(type: "bytea", nullable: false),
                    generatedutc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Embeddings", x => x.id);
                    table.ForeignKey(
                        name: "FK_Embeddings_Definitions_DefinitionId",
                        column: x => x.definitionid,
                        principalSchema: "dbo",
                        principalTable: "definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactInformationElements_DefinitionId",
                schema: "dbo",
                table: "artifactinformationelements",
                column: "definitionid");

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactInformationElements_Organization_ArtifactId",
                schema: "dbo",
                table: "artifactinformationelements",
                columns: new[] { "ownerorganizationid", "artifactid" });

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactInformationElements_Organization_DefinitionId",
                schema: "dbo",
                table: "artifactinformationelements",
                columns: new[] { "ownerorganizationid", "definitionid" });

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactInformationElements_OwnerOrganizationId",
                schema: "dbo",
                table: "artifactinformationelements",
                column: "ownerorganizationid");

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_Organization_ArchetypeKey",
                schema: "dbo",
                table: "artifacts",
                columns: new[] { "ownerorganizationid", "archetypekey" });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_Organization_ProductionCardinalityKey",
                schema: "dbo",
                table: "artifacts",
                columns: new[] { "ownerorganizationid", "productioncardinalitykey" });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_Organization_ScopeTypeKey",
                schema: "dbo",
                table: "artifacts",
                columns: new[] { "ownerorganizationid", "scopetypekey" });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_Organization_StatusKey",
                schema: "dbo",
                table: "artifacts",
                columns: new[] { "ownerorganizationid", "statuskey" });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_OwnerOrganizationId",
                schema: "dbo",
                table: "artifacts",
                column: "ownerorganizationid");

            migrationBuilder.CreateIndex(
                name: "UX_Artifacts_Organization_Key",
                schema: "dbo",
                table: "artifacts",
                columns: new[] { "ownerorganizationid", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Artifacts_Organization_Tla",
                schema: "dbo",
                table: "artifacts",
                columns: new[] { "ownerorganizationid", "tla" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Concepts_OwnerOrganizationId",
                schema: "dbo",
                table: "concepts",
                column: "ownerorganizationid");

            migrationBuilder.CreateIndex(
                name: "UX_Concepts_Key",
                schema: "dbo",
                table: "concepts",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Definitions_ConceptId",
                schema: "dbo",
                table: "definitions",
                column: "conceptid");

            migrationBuilder.CreateIndex(
                name: "IX_Definitions_OwnerOrganizationId",
                schema: "dbo",
                table: "definitions",
                column: "ownerorganizationid");

            migrationBuilder.CreateIndex(
                name: "IX_Definitions_SubjectId",
                schema: "dbo",
                table: "definitions",
                column: "subjectid");

            migrationBuilder.CreateIndex(
                name: "UX_Definitions_QualifiedKey",
                schema: "dbo",
                table: "definitions",
                column: "qualifiedkey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_OwnerOrganizationId",
                schema: "dbo",
                table: "embeddings",
                column: "ownerorganizationid");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_SourceSha256",
                schema: "dbo",
                table: "embeddings",
                column: "sourcesha256");

            migrationBuilder.CreateIndex(
                name: "UX_Embeddings_DefinitionId_ModelKey",
                schema: "dbo",
                table: "embeddings",
                columns: new[] { "definitionid", "modelkey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registry_OwnerOrganizationId",
                schema: "dbo",
                table: "registry",
                column: "ownerorganizationid");

            migrationBuilder.CreateIndex(
                name: "UX_Registry_Name",
                schema: "dbo",
                table: "registry",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_OwnerOrganizationId",
                schema: "dbo",
                table: "subjects",
                column: "ownerorganizationid");

            migrationBuilder.CreateIndex(
                name: "UX_Subjects_Key",
                schema: "dbo",
                table: "subjects",
                column: "key",
                unique: true);
            migrationBuilder.Sql(@"CREATE VIEW dbo.usv_semanticdefinitions AS
SELECT d.id, d.ownerorganizationid, d.subjectid,
       s.key AS subjectkey, s.name AS subjectname, s.description AS subjectdescription,
       d.conceptid, c.key AS conceptkey, c.name AS conceptname, c.description AS conceptdescription,
       d.qualifiedkey, d.name, d.summary, d.example1, d.example2, d.example3,
       d.statuskey, d.definitionsha256, d.creationdate, d.lastupdateddate
FROM dbo.definitions d
JOIN dbo.subjects s ON s.id = d.subjectid
JOIN dbo.concepts c ON c.id = d.conceptid;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.usv_semanticdefinitions;");
            migrationBuilder.DropTable(
                name: "artifactinformationelements",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "embeddings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "registry",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "artifacts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "definitions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "concepts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "subjects",
                schema: "dbo");
        }
    }
}
