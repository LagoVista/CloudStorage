// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 23735cb269e7c3db15d072d6a7fd20ddc397658f1f09108401e6a175cfaed758
// IndexVersion: 1
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Models
{
    public class TableStorageResultSet<TEntity> where TEntity : TableStorageEntity
    {
        [JsonProperty("odata.metadata")]
        public string MetaData { get; set; }

        [JsonProperty("value")]
        public List<TEntity> ResultSet { get; set; }
    }
}
