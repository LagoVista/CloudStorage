using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Models
{
    internal class EntityHeaderRow
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("EntityType")]
        public string EntityType { get; set; }

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("Namespace")]
        public string Namespace { get; set; }

        [JsonProperty("Eemail")]
        public string Eemail { get; set; }

        [JsonProperty("OwnerOrganization")]
        public EntityHeader OwnerOrganization { get; set; }

        [JsonProperty("IsPublic")]
        public bool IsPublic { get; set; }

        public string GetKey()
        {
            if (!String.IsNullOrEmpty(Namespace))
                return Namespace;

            if (!String.IsNullOrEmpty(UserName))
                return CosmosSyncRepository.NormalizeAlphaNumericKey(UserName);

            if (String.IsNullOrEmpty(Key))
            {
                if (!String.IsNullOrEmpty(Eemail))
                    return CosmosSyncRepository.NormalizeAlphaNumericKey(Eemail);
                else
                    return Id.ToLower();
            }

            return Key;
        }
    }
}
