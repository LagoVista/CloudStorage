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
