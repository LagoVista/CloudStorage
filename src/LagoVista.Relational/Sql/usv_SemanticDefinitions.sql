CREATE OR ALTER VIEW [dbo].[usv_SemanticDefinitions]
AS
SELECT
    d.[Id],
    d.[OwnerOrganizationId],
    d.[SubjectId],
    s.[Key] AS [SubjectKey],
    s.[Name] AS [SubjectName],
    s.[Description] AS [SubjectDescription],
    d.[ConceptId],
    c.[Key] AS [ConceptKey],
    c.[Name] AS [ConceptName],
    c.[Description] AS [ConceptDescription],
    d.[QualifiedKey],
    d.[Name],
    d.[Summary],
    d.[Example1],
    d.[Example2],
    d.[Example3],
    d.[StatusKey],
    d.[DefinitionSha256],
    d.[CreationDate],
    d.[LastUpdatedDate]
FROM [dbo].[Definitions] d
INNER JOIN [dbo].[Subjects] s ON s.[Id] = d.[SubjectId]
INNER JOIN [dbo].[Concepts] c ON c.[Id] = d.[ConceptId];
