// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: d7bdcd4d30343802ef72282811a8161b3b55c6aafb9748b62542aecdf587cbeb
// IndexVersion: 1
// --- END CODE INDEX META ---
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Exceptions
{
    public class ContentModifiedException : Exception
    {
        public string EntityType { get; set; }
        public string Id { get; set; }
    }
}
