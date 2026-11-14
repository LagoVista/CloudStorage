using System;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LagoVista.Relational.Migrations.SemanticPostgres
{
    [DbContext(typeof(SemanticPostgresMigrationContext))]
    [Migration("20261114121500_MoveSemanticToPublicSchema")]
    public partial class MoveSemanticToPublicSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL moves indexes with tables; dependencies follow object IDs.
            migrationBuilder.Sql(@"ALTER TABLE dbo.registry SET SCHEMA public;
ALTER TABLE dbo.subjects SET SCHEMA public;
ALTER TABLE dbo.concepts SET SCHEMA public;
ALTER TABLE dbo.definitions SET SCHEMA public;
ALTER TABLE dbo.embeddings SET SCHEMA public;
ALTER TABLE dbo.artifacts SET SCHEMA public;
ALTER TABLE dbo.artifactinformationelements SET SCHEMA public;
ALTER VIEW dbo.usv_semanticdefinitions SET SCHEMA public;
DROP SCHEMA dbo RESTRICT;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Semantic PostgreSQL migrations are forward-only.");
        }
    }
}
