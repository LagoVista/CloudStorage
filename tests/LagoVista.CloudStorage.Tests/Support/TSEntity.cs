// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 0367c280c8bd1ca7ad8d89efa319a2af33e746b1c69923ea81417d8f97f6e387
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests.Support
{
    public class TSEntity : TableStorageEntity
    {
        public int Index { get; set; }

        public string Value1 { get; set; }
        public string Value2 { get; set; }
    }
}
