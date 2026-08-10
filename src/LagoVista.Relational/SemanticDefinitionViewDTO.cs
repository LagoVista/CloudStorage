using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational
{
    public class SemanticDefinitionViewDTO
    {
        public string Id { get; set; }
        public string OwnerOrganizationId { get; set; }

        public string SubjectId { get; set; }
        public string SubjectKey { get; set; }
        public string SubjectName { get; set; }
        public string SubjectDescription { get; set; }

        public string ConceptId { get; set; }
        public string ConceptKey { get; set; }
        public string ConceptName { get; set; }
        public string ConceptDescription { get; set; }

        public string QualifiedKey { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Example1 { get; set; }
        public string Example2 { get; set; }
        public string Example3 { get; set; }
        public string StatusKey { get; set; }
        public string DefinitionSha256 { get; set; }
        public System.DateTime CreationDate { get; set; }
        public System.DateTime LastUpdatedDate { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SemanticDefinitionViewDTO>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("usv_SemanticDefinitions", "dbo");
            });
        }
    }
}
