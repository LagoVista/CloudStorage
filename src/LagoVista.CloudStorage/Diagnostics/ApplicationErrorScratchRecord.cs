using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Models;

namespace LagoVista.CloudStorage.Diagnostics
{
    internal sealed class ApplicationErrorScratchRecord : IScratchDataRecord
    {
        public NormalizedId32 Id { get; set; }

        public EntityHeader Organization { get; set; }

        public LogRecord Record { get; set; }
    }
}
