IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[Artifacts] (
    [Id] varchar(32) NOT NULL,
    [OwnerOrganizationId] varchar(32) NOT NULL,
    [Key] varchar(64) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Tla] varchar(16) NOT NULL,
    [Description] nvarchar(1024) NULL,
    [PurposeSummary] nvarchar(1024) NULL,
    [StatusKey] nvarchar(50) NOT NULL,
    [ScopeTypeKey] varchar(64) NOT NULL,
    [ArchetypeKey] varchar(64) NOT NULL,
    [ProductionCardinalityKey] varchar(64) NOT NULL,
    [SpecificationSha256] varchar(64) NOT NULL,
    [CreationDate] datetime2(7) NOT NULL,
    [LastUpdatedDate] datetime2(7) NOT NULL,
    CONSTRAINT [PK_Artifacts] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[Concepts] (
    [Id] varchar(32) NOT NULL,
    [OwnerOrganizationId] varchar(32) NOT NULL,
    [Key] varchar(64) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Description] nvarchar(1024) NULL,
    [Aliases] nvarchar(1024) NULL,
    [StatusKey] nvarchar(50) NOT NULL,
    [CreationDate] datetime2(7) NOT NULL,
    [LastUpdatedDate] datetime2(7) NOT NULL,
    CONSTRAINT [PK_Concepts] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[Registry] (
    [Id] uniqueidentifier NOT NULL,
    [OwnerOrganizationId] varchar(32) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Revision] bigint NOT NULL,
    [SeededFromRevision] bigint NULL,
    [SeededUtc] datetime2(7) NULL,
    [CreatedUtc] datetime2(7) NOT NULL,
    [UpdatedUtc] datetime2(7) NOT NULL,
    CONSTRAINT [PK_Registry] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[Subjects] (
    [Id] varchar(32) NOT NULL,
    [OwnerOrganizationId] varchar(32) NOT NULL,
    [Key] varchar(64) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Description] nvarchar(1024) NULL,
    [Aliases] nvarchar(1024) NULL,
    [StatusKey] nvarchar(50) NOT NULL,
    [CreationDate] datetime2(7) NOT NULL,
    [LastUpdatedDate] datetime2(7) NOT NULL,
    CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[Definitions] (
    [Id] varchar(32) NOT NULL,
    [OwnerOrganizationId] varchar(32) NOT NULL,
    [SubjectId] varchar(32) NOT NULL,
    [ConceptId] varchar(32) NOT NULL,
    [QualifiedKey] nvarchar(128) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Summary] nvarchar(2048) NOT NULL,
    [Example1] nvarchar(1024) NULL,
    [Example2] nvarchar(1024) NULL,
    [Example3] nvarchar(1024) NULL,
    [StatusKey] nvarchar(50) NOT NULL,
    [DefinitionSha256] varchar(64) NOT NULL,
    [CreationDate] datetime2(7) NOT NULL,
    [LastUpdatedDate] datetime2(7) NOT NULL,
    CONSTRAINT [PK_Definitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Definitions_Concepts_ConceptId] FOREIGN KEY ([ConceptId]) REFERENCES [dbo].[Concepts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Definitions_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [dbo].[Subjects] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[ArtifactInformationElements] (
    [ArtifactId] varchar(32) NOT NULL,
    [DefinitionId] varchar(32) NOT NULL,
    [OwnerOrganizationId] varchar(32) NOT NULL,
    [UsageRole] nvarchar(50) NULL,
    [CreationDate] datetime2(7) NOT NULL,
    [LastUpdatedDate] datetime2(7) NOT NULL,
    CONSTRAINT [PK_ArtifactInformationElements] PRIMARY KEY ([ArtifactId], [DefinitionId]),
    CONSTRAINT [FK_ArtifactInformationElements_Artifacts_ArtifactId] FOREIGN KEY ([ArtifactId]) REFERENCES [dbo].[Artifacts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ArtifactInformationElements_Definitions_DefinitionId] FOREIGN KEY ([DefinitionId]) REFERENCES [dbo].[Definitions] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[Embeddings] (
    [Id] uniqueidentifier NOT NULL,
    [OwnerOrganizationId] varchar(32) NOT NULL,
    [DefinitionId] varchar(32) NOT NULL,
    [ModelKey] nvarchar(128) NOT NULL,
    [Dimensions] int NOT NULL,
    [SourceSha256] varchar(64) NOT NULL,
    [Vector] varbinary(max) NOT NULL,
    [GeneratedUtc] datetime2(7) NOT NULL,
    CONSTRAINT [PK_Embeddings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Embeddings_Definitions_DefinitionId] FOREIGN KEY ([DefinitionId]) REFERENCES [dbo].[Definitions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ArtifactInformationElements_DefinitionId] ON [dbo].[ArtifactInformationElements] ([DefinitionId]);

CREATE INDEX [IX_ArtifactInformationElements_Organization_ArtifactId] ON [dbo].[ArtifactInformationElements] ([OwnerOrganizationId], [ArtifactId]);

CREATE INDEX [IX_ArtifactInformationElements_Organization_DefinitionId] ON [dbo].[ArtifactInformationElements] ([OwnerOrganizationId], [DefinitionId]);

CREATE INDEX [IX_ArtifactInformationElements_OwnerOrganizationId] ON [dbo].[ArtifactInformationElements] ([OwnerOrganizationId]);

CREATE INDEX [IX_Artifacts_Organization_ArchetypeKey] ON [dbo].[Artifacts] ([OwnerOrganizationId], [ArchetypeKey]);

CREATE INDEX [IX_Artifacts_Organization_ProductionCardinalityKey] ON [dbo].[Artifacts] ([OwnerOrganizationId], [ProductionCardinalityKey]);

CREATE INDEX [IX_Artifacts_Organization_ScopeTypeKey] ON [dbo].[Artifacts] ([OwnerOrganizationId], [ScopeTypeKey]);

CREATE INDEX [IX_Artifacts_Organization_StatusKey] ON [dbo].[Artifacts] ([OwnerOrganizationId], [StatusKey]);

CREATE INDEX [IX_Artifacts_OwnerOrganizationId] ON [dbo].[Artifacts] ([OwnerOrganizationId]);

CREATE UNIQUE INDEX [UX_Artifacts_Organization_Key] ON [dbo].[Artifacts] ([OwnerOrganizationId], [Key]);

CREATE UNIQUE INDEX [UX_Artifacts_Organization_Tla] ON [dbo].[Artifacts] ([OwnerOrganizationId], [Tla]);

CREATE INDEX [IX_Concepts_OwnerOrganizationId] ON [dbo].[Concepts] ([OwnerOrganizationId]);

CREATE UNIQUE INDEX [UX_Concepts_Key] ON [dbo].[Concepts] ([Key]);

CREATE INDEX [IX_Definitions_ConceptId] ON [dbo].[Definitions] ([ConceptId]);

CREATE INDEX [IX_Definitions_OwnerOrganizationId] ON [dbo].[Definitions] ([OwnerOrganizationId]);

CREATE INDEX [IX_Definitions_SubjectId] ON [dbo].[Definitions] ([SubjectId]);

CREATE UNIQUE INDEX [UX_Definitions_QualifiedKey] ON [dbo].[Definitions] ([QualifiedKey]);

CREATE INDEX [IX_Embeddings_OwnerOrganizationId] ON [dbo].[Embeddings] ([OwnerOrganizationId]);

CREATE INDEX [IX_Embeddings_SourceSha256] ON [dbo].[Embeddings] ([SourceSha256]);

CREATE UNIQUE INDEX [UX_Embeddings_DefinitionId_ModelKey] ON [dbo].[Embeddings] ([DefinitionId], [ModelKey]);

CREATE INDEX [IX_Registry_OwnerOrganizationId] ON [dbo].[Registry] ([OwnerOrganizationId]);

CREATE UNIQUE INDEX [UX_Registry_Name] ON [dbo].[Registry] ([Name]);

CREATE INDEX [IX_Subjects_OwnerOrganizationId] ON [dbo].[Subjects] ([OwnerOrganizationId]);

CREATE UNIQUE INDEX [UX_Subjects_Key] ON [dbo].[Subjects] ([Key]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260811135942_InitialSemanticCatalog', N'9.0.13');

COMMIT;
GO

