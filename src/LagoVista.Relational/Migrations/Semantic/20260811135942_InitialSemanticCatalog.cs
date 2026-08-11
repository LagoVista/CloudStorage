using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LagoVista.Relational.Migrations.Semantic
{
    /// <inheritdoc />
    public partial class InitialSemanticCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Artifacts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(32)", nullable: false),
                    OwnerOrganizationId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Key = table.Column<string>(type: "varchar(64)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Tla = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    PurposeSummary = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    StatusKey = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ScopeTypeKey = table.Column<string>(type: "varchar(64)", nullable: false),
                    ArchetypeKey = table.Column<string>(type: "varchar(64)", nullable: false),
                    ProductionCardinalityKey = table.Column<string>(type: "varchar(64)", nullable: false),
                    SpecificationSha256 = table.Column<string>(type: "varchar(64)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Concepts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(32)", nullable: false),
                    OwnerOrganizationId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Key = table.Column<string>(type: "varchar(64)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    Aliases = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    StatusKey = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concepts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Registry",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerOrganizationId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    SeededFromRevision = table.Column<long>(type: "bigint", nullable: true),
                    SeededUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(32)", nullable: false),
                    OwnerOrganizationId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Key = table.Column<string>(type: "varchar(64)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    Aliases = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    StatusKey = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Definitions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(32)", nullable: false),
                    OwnerOrganizationId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    SubjectId = table.Column<string>(type: "varchar(32)", nullable: false),
                    ConceptId = table.Column<string>(type: "varchar(32)", nullable: false),
                    QualifiedKey = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2048)", nullable: false),
                    Example1 = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    Example2 = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    Example3 = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    StatusKey = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    DefinitionSha256 = table.Column<string>(type: "varchar(64)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Definitions_Concepts_ConceptId",
                        column: x => x.ConceptId,
                        principalSchema: "dbo",
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Definitions_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "dbo",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArtifactInformationElements",
                schema: "dbo",
                columns: table => new
                {
                    ArtifactId = table.Column<string>(type: "varchar(32)", nullable: false),
                    DefinitionId = table.Column<string>(type: "varchar(32)", nullable: false),
                    OwnerOrganizationId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    UsageRole = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactInformationElements", x => new { x.ArtifactId, x.DefinitionId });
                    table.ForeignKey(
                        name: "FK_ArtifactInformationElements_Artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalSchema: "dbo",
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtifactInformationElements_Definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalSchema: "dbo",
                        principalTable: "Definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Embeddings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerOrganizationId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    DefinitionId = table.Column<string>(type: "varchar(32)", nullable: false),
                    ModelKey = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    Dimensions = table.Column<int>(type: "int", nullable: false),
                    SourceSha256 = table.Column<string>(type: "varchar(64)", nullable: false),
                    Vector = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    GeneratedUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Embeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Embeddings_Definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalSchema: "dbo",
                        principalTable: "Definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactInformationElements_DefinitionId",
                schema: "dbo",
                table: "ArtifactInformationElements",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactInformationElements_Organization_ArtifactId",
                schema: "dbo",
                table: "ArtifactInformationElements",
                columns: new[] { "OwnerOrganizationId", "ArtifactId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactInformationElements_Organization_DefinitionId",
                schema: "dbo",
                table: "ArtifactInformationElements",
                columns: new[] { "OwnerOrganizationId", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactInformationElements_OwnerOrganizationId",
                schema: "dbo",
                table: "ArtifactInformationElements",
                column: "OwnerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_Organization_ArchetypeKey",
                schema: "dbo",
                table: "Artifacts",
                columns: new[] { "OwnerOrganizationId", "ArchetypeKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_Organization_ProductionCardinalityKey",
                schema: "dbo",
                table: "Artifacts",
                columns: new[] { "OwnerOrganizationId", "ProductionCardinalityKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_Organization_ScopeTypeKey",
                schema: "dbo",
                table: "Artifacts",
                columns: new[] { "OwnerOrganizationId", "ScopeTypeKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_Organization_StatusKey",
                schema: "dbo",
                table: "Artifacts",
                columns: new[] { "OwnerOrganizationId", "StatusKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_OwnerOrganizationId",
                schema: "dbo",
                table: "Artifacts",
                column: "OwnerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "UX_Artifacts_Organization_Key",
                schema: "dbo",
                table: "Artifacts",
                columns: new[] { "OwnerOrganizationId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Artifacts_Organization_Tla",
                schema: "dbo",
                table: "Artifacts",
                columns: new[] { "OwnerOrganizationId", "Tla" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Concepts_OwnerOrganizationId",
                schema: "dbo",
                table: "Concepts",
                column: "OwnerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "UX_Concepts_Key",
                schema: "dbo",
                table: "Concepts",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Definitions_ConceptId",
                schema: "dbo",
                table: "Definitions",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_Definitions_OwnerOrganizationId",
                schema: "dbo",
                table: "Definitions",
                column: "OwnerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Definitions_SubjectId",
                schema: "dbo",
                table: "Definitions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "UX_Definitions_QualifiedKey",
                schema: "dbo",
                table: "Definitions",
                column: "QualifiedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_OwnerOrganizationId",
                schema: "dbo",
                table: "Embeddings",
                column: "OwnerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_SourceSha256",
                schema: "dbo",
                table: "Embeddings",
                column: "SourceSha256");

            migrationBuilder.CreateIndex(
                name: "UX_Embeddings_DefinitionId_ModelKey",
                schema: "dbo",
                table: "Embeddings",
                columns: new[] { "DefinitionId", "ModelKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registry_OwnerOrganizationId",
                schema: "dbo",
                table: "Registry",
                column: "OwnerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "UX_Registry_Name",
                schema: "dbo",
                table: "Registry",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_OwnerOrganizationId",
                schema: "dbo",
                table: "Subjects",
                column: "OwnerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "UX_Subjects_Key",
                schema: "dbo",
                table: "Subjects",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtifactInformationElements",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Embeddings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Registry",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Artifacts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Definitions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Concepts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Subjects",
                schema: "dbo");
        }
    }
}
