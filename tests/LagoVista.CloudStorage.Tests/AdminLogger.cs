using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    public class AdminLogger : IAdminLogger
    {
        public bool DebugMode { get; set; }

        public void AddConfigurationError(string tag, string message, params KeyValuePair<string, string>[] args)
        {
            Console.WriteLine($"{tag} - {message}");
            foreach(var arg in args)
                Console.WriteLine($"\t\t{arg.Key} - {arg.Value}");
        }

        public void AddCustomEvent(LogLevel level, string tag, string customEvent, params KeyValuePair<string, string>[] args)
        {
            Console.WriteLine($"[{level}] {tag} - {customEvent}");
            foreach (var arg in args)
                Console.WriteLine($"\t\t{arg.Key} - {arg.Value}");
        }

        public void AddError(string tag, string message, params KeyValuePair<string, string>[] args)
        {
            Console.WriteLine($"[ERROR] {tag} - {message}");
            foreach (var arg in args)
                Console.WriteLine($"\t\t{arg.Key} - {arg.Value}");
        }

        public void AddError(ErrorCode errorCode, params KeyValuePair<string, string>[] args)
        {
            Console.WriteLine($"[ERROR] {errorCode.Code} - {errorCode.Message}");
            foreach (var arg in args)
                Console.WriteLine($"\t\t{arg.Key} - {arg.Value}");
        }

        public void AddException(string tag, Exception ex, params KeyValuePair<string, string>[] args)
        {
            Console.WriteLine($"[ERROR] {tag} - {ex.Message}");
            foreach (var arg in args)
                Console.WriteLine($"\t\t{arg.Key} - {arg.Value}");
        }

        public void AddKVPs(params KeyValuePair<string, string>[] args)
        {
            
        }

        public void AddMetric(string measure, double duration)
        {
            
        }

        public void AddMetric(string measure, int count)
        {
            
        }

        public void EndTimedEvent(TimedEvent evt)
        {
        
        }

        public void LogInvokeResult(string tag, InvokeResult result, params KeyValuePair<string, string>[] args)
        {
            Console.WriteLine($"[ERROR] {tag} - {result.Errors.First().Message}");
            foreach (var arg in args)
                Console.WriteLine($"\t\t{arg.Key} - {arg.Value}");
        }

        public void LogInvokeResult<TResultType>(string tag, InvokeResult<TResultType> result, params KeyValuePair<string, string>[] args)
        {
            Console.WriteLine($"[ERROR] {tag} - {result.Errors.First().Message}");
            foreach (var arg in args)
                Console.WriteLine($"\t\t{arg.Key} - {arg.Value}");
        }

        public TimedEvent StartTimedEvent(string area, string description)
        {
            return new TimedEvent(area, description);
        }

        public void TrackEvent(string message, Dictionary<string, string> parameters)
        {
            
        }

        public void TrackMetric(string kind, string name, MetricType metricType, double count, params KeyValuePair<string, string>[] args)
        {
            
        }

        public void TrackMetric(string kind, string name, MetricType metricType, int count, params KeyValuePair<string, string>[] args)
        {
            
        }
    }
}
