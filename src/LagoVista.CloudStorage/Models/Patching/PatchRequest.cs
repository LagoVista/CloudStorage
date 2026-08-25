using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    public class PatchRequest
    {

        public string Id { get; set; }
        public string EntityType { get; set; }
        
        public string PartitionKey { get; set; }
        public string ETag { get; set; }
        public IReadOnlyList<PatchStep> Steps { get; set; }
    }
}
