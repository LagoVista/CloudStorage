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
