using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Diagnostics
{
    /// <summary>Append-only representation of a LogRecord using Cassandra-supported scalar columns.</summary>
    public sealed class DiagnosticLogActivityRecord : IActivityRecord
    {
        public const string SystemOrganizationId = "nuviot-diagnostics";
        public const string SystemOrganizationName = "NuvIoT Diagnostics";

        public string Id { get; set; }
        public string OrganizationId { get; set; }
        public string Organization { get; set; }
        public DateTime CreationDate { get; set; }

        public string Application { get; set; }
        public string Environment { get; set; }
        public string LogLevel { get; set; }
        public string Tag { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string StackTrace { get; set; }
        public string ExceptionType { get; set; }
        public string HostId { get; set; }
        public string Version { get; set; }
        public string PemId { get; set; }
        public string InstanceId { get; set; }
        public string PipelineModuleId { get; set; }
        public string DeviceTypeId { get; set; }
        public string DeviceId { get; set; }
        public string ActivityId { get; set; }
        public string ErrorCode { get; set; }
        public string Area { get; set; }
        public double Measure { get; set; }
        public double MS { get; set; }
        public string OldState { get; set; }
        public string NewState { get; set; }
        public bool EscapeCRLF { get; set; }
        public string ParametersJson { get; set; }

        public static DiagnosticLogActivityRecord FromLogRecord(LogRecord record, string application, string environment, string version)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            var timestamp = record.TimeStamp == default(DateTime) ? DateTime.UtcNow : record.TimeStamp;
            if (timestamp.Kind == DateTimeKind.Local) timestamp = timestamp.ToUniversalTime();
            else if (timestamp.Kind == DateTimeKind.Unspecified) timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);

            return new DiagnosticLogActivityRecord
            {
                Id = String.IsNullOrWhiteSpace(record.Id.ToString()) ? Guid.NewGuid().ToString("N").ToUpperInvariant() : record.Id.ToString(),
                OrganizationId = SystemOrganizationId,
                Organization = SystemOrganizationName,
                CreationDate = timestamp,
                Application = String.IsNullOrWhiteSpace(record.Application) ? application : record.Application,
                Environment = String.IsNullOrWhiteSpace(record.Environment) ? environment : record.Environment,
                LogLevel = record.LogLevel,
                Tag = record.Tag,
                Message = record.Message,
                Details = record.Details,
                StackTrace = record.StackTrace,
                ExceptionType = record.ExceptionType,
                HostId = record.HostId,
                Version = String.IsNullOrWhiteSpace(record.Version) ? version : record.Version,
                PemId = record.PemId,
                InstanceId = record.InstanceId,
                PipelineModuleId = record.PipelineModuleId,
                DeviceTypeId = record.DeviceTypeId,
                DeviceId = record.DeviceId,
                ActivityId = record.ActivityId,
                ErrorCode = record.ErrorCode,
                Area = record.Area,
                Measure = record.Measure,
                MS = record.MS,
                OldState = record.OldState,
                NewState = record.NewState,
                EscapeCRLF = record.EscapeCRLF,
                ParametersJson = JsonConvert.SerializeObject(record.Parameters ?? new Dictionary<string, string>())
            };
        }

        public LogRecord ToLogRecord()
        {
            return new LogRecord
            {
                Id = new NormalizedId32(Id),
                TimeStamp = CreationDate,
                Application = Application,
                Environment = Environment,
                LogLevel = LogLevel,
                Tag = Tag,
                Message = Message,
                Details = Details,
                StackTrace = StackTrace,
                ExceptionType = ExceptionType,
                HostId = HostId,
                Version = Version,
                PemId = PemId,
                InstanceId = InstanceId,
                PipelineModuleId = PipelineModuleId,
                DeviceTypeId = DeviceTypeId,
                DeviceId = DeviceId,
                ActivityId = ActivityId,
                ErrorCode = ErrorCode,
                Area = Area,
                Measure = Measure,
                MS = MS,
                OldState = OldState,
                NewState = NewState,
                EscapeCRLF = EscapeCRLF,
                Parameters = String.IsNullOrWhiteSpace(ParametersJson)
                    ? new Dictionary<string, string>()
                    : JsonConvert.DeserializeObject<Dictionary<string, string>>(ParametersJson) ?? new Dictionary<string, string>()
            };
        }
    }
}
