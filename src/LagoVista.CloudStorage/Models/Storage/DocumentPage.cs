using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Models.Storage
{
    public class DocumentPage<TProjection>
    {
        public IReadOnlyList<TProjection> Items { get; set; }

        public string ContinuationToken { get; set; }
    }
}
