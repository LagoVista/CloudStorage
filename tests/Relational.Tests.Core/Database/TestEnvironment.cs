using LagoVista.Core.Interfaces;
using LagoVista.Core.Interfaces.AutoMapper;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.Relational.DataContexts;
using System;
using System.Threading.Tasks;

namespace Relational.Tests.Core.Database
{
    public sealed class TestEnvironment : IAsyncDisposable
    {
        public BillingDataContext Billing { get; }

        public MetricsDataContext Metrics { get; }

        public IAdminLogger Logger { get; }
        public ILagoVistaAutoMapper AutoMapper { get; }
        public ISecureStorage SecureStorage { get; }


        public TestEnvironment(
            BillingDataContext billing,
            MetricsDataContext metrics,
            IAdminLogger logger,
            ILagoVistaAutoMapper autoMapper,
            ISecureStorage secureStorage)
        {
            Billing = billing;
            Logger = logger;
            Metrics = metrics;
            AutoMapper = autoMapper;
            SecureStorage = secureStorage;
        }

        public async ValueTask DisposeAsync()
        {
            if (Billing != null)
                await Billing.DisposeAsync();

            if (Metrics != null)
                await Metrics.DisposeAsync();
        }
    }
}
