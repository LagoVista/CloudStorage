using LagoVista.Core;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class TimePeriodDTO
    {
        [Key]
        public Guid Id { get; set; }

        public string OrganizationId { get; set; }

        public int Year { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool Locked { get; set; }

        public Guid? PayrollSummaryId { get; set; }
        public PayrollSummaryDTO PayrollSummary { get; set; }

        public DateTime? LockedTimeStamp { get; set; }
        public string LockedByUserId { get; set; }
    }

}
