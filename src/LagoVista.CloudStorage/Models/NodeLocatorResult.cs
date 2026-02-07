using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    public class NodeLocatorResult
    {
        public string ContinuationToken { get; set; }
        public List<NodeLocatorEntry> Entries { get; set; } = new List<NodeLocatorEntry>();
    }
}
