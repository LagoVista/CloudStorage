using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using System;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging;
using System.Collections.Generic;
using LagoVista.Core.Models.UIMetaData;
using System.Threading.Tasks;

namespace ThrowAway
{
    public class UsageMetricsRepo : TableStorageBase<UsageMetrics>
    {
        public UsageMetricsRepo(String accountName, string accountKey, IAdminLogger logger) : base(accountName, accountKey, logger)
        {

        }

        public Task<ListResponse<UsageMetrics>> GetByPage(String id, ListRequest listRequest)
        {
            return GetPagedResultsAsync(id, listRequest);
        }
    }

    public class JunkLogger : IAdminLogger
    {
        public bool DebugMode { get; set; }

        public void AddConfigurationError(string tag, string message, params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }

        public void AddCustomEvent(LogLevel level, string tag, string customEvent, params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }

        public void AddError(string tag, string message, params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }

        public void AddError(ErrorCode errorCode, params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }

        public void AddException(string tag, Exception ex, params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }

        public void AddKVPs(params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }

        public void AddMetric(string measure, double duration)
        {
            throw new NotImplementedException();
        }

        public void AddMetric(string measure, int count)
        {
            throw new NotImplementedException();
        }

        public void EndTimedEvent(TimedEvent evt)
        {
            throw new NotImplementedException();
        }

        public void LogInvokeResult(string tag, InvokeResult result, params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }

        public void LogInvokeResult<TResultType>(string tag, InvokeResult<TResultType> result, params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }

        public TimedEvent StartTimedEvent(string area, string description)
        {
            throw new NotImplementedException();
        }

        public void TrackEvent(string message, Dictionary<string, string> parameters)
        {
            throw new NotImplementedException();
        }

        public void TrackMetric(string kind, string name, MetricType metricType, double count, params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }

        public void TrackMetric(string kind, string name, MetricType metricType, int count, params KeyValuePair<string, string>[] args)
        {
            throw new NotImplementedException();
        }
    }
}