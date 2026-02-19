
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational.DataContexts
{
    [Table("metrics", Schema = "public")]
    public class MetricsDTO
    {
        [Key]
        public DateTimeOffset Time { get; set; }

        [Required] public string MetricName { get; set; } = default!;
        [Required] public string MetricId { get; set; } = default!;
        [Required] public string Span { get; set; } = default!; // char(1)

        [Required] public string OrgId { get; set; } = default!;
        [Required] public string OrgName { get; set; } = default!;

        public string? UserId { get; set; }
        public string? UserName { get; set; }

        public string? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public string? Attr1Id { get; set; }
        public string? Attr1 { get; set; }

        public string? Attr2Id { get; set; }
        public string? Attr2 { get; set; }

        public string? Attr3Id { get; set; }
        public string? Attr3 { get; set; }

        public string? Attr4Id { get; set; }
        public string? Attr4 { get; set; }

        public string? Attr5Id { get; set; }
        public string? Attr5 { get; set; }

        public double Value { get; set; }

        public string? Attr6Id { get; set; }
        public string? Attr6 { get; set; }

        public string? Attr7Id { get; set; }
        public string? Attr7 { get; set; }

        public string? Attr8Id { get; set; }
        public string? Attr8 { get; set; }
    }
}

