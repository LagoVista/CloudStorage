using Microsoft.EntityFrameworkCore;

namespace LagoVista.Relational.DataContexts
{
    public class MetricsDataContext :  DbContext
    {
        public MetricsDataContext(DbContextOptions<MetricsDataContext> optionsContext) :
           base(optionsContext)
        {

        }

        public DbSet<MetricsDTO> Metrics { get; set; }
        public DbSet<MetricDefinitionDTO> MetricsDefinitions { get; set; }

    }
}
