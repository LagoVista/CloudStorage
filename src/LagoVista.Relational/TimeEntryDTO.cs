using LagoVista.Core;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class TimeEntryDTO : DbModelBase
    {
        public Guid AgreementId { get; set; }
        public Guid TimePeriodId { get; set; }
        public Guid? BillingEventId { get; set; }
        public DateTime Date { get; set; }

        public string OrganizationId { get; set; }
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string WorkTaskId { get; set; }
        public string WorkTaskName { get; set; }
        public string UserId { get; set; }
        public bool Locked { get; set; }
        public bool IsEquityTime { get; set; }
        public decimal Hours { get; set; }
        public string Notes { get; set; }
    }
}
