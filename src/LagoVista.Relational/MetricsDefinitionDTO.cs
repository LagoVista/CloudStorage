using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [Table("metrics_definition", Schema = "public")]
    public class MetricDefinitionDTO
    {
        [Key]
        public string Id { get; set; } = default!; // character(32)

        [Required] public string Name { get; set; } = default!;
        [Required] public string Summary { get; set; } = default!;
        [Required] public string Help { get; set; } = default!;
        [Required] public string Description { get; set; } = default!;
        [Required] public string Key { get; set; } = default!;
        [Required] public string Icon { get; set; } = default!;

        [Required] public string CategoryId { get; set; } = default!;
        [Required] public string CategoryKey { get; set; } = default!;
        [Required] public string CategoryName { get; set; } = default!;

        public string Attr1Name { get; set; }
        public string Attr1Key { get; set; }

        public string Attr2Name { get; set; }
        public string Attr2Key { get; set; }

        public string Attr3Name { get; set; }
        public string Attr3Key { get; set; }

        public string Attr4Name { get; set; }
        public string Attr4Key { get; set; }

        public string Attr5Name { get; set; }
        public string Attr5Key { get; set; }

        public string Attr6Name { get; set; }
        public string Attr6Key { get; set; }

        public string Attr7Name { get; set; }
        public string Attr7Key { get; set; }

        public string Attr8Name { get; set; }
        public string Attr8Key { get; set; }

        [Required]
        public bool ReadOnly { get; set; }
    }
}


