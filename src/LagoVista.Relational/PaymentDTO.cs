using LagoVista.Core.Attributes;
using LagoVista.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LagoVista.Relational
{
    [EncryptionKey("PAYROLL_KEY")]
    public class PaymentDTO : DbModelBase
    {
        public const string PaymentStatus_New = "new";
        public const string PaymentStatus_Approved = "approved";
        public const string PaymentStatus_Funded = "funded";

        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }

        [IgnoreOnMapTo]
        public string UserId { get; set; }

        public Guid TimePeriodId { get; set; }

        public string Status { get; set; }

        public DateOnly? SubmittedDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }

        public decimal BillableHours { get; set; }
        public decimal InternalHours { get; set; }

       
        public string Gross { get; set; }
        public string Net { get; set; }
        public string Expenses { get; set; }

        public decimal EquityHours { get; set; }
        public string PrimaryTransactionId { get; set; }
        public string PrimaryDeposit { get; set; }
        public string SecondaryTransactionId { get; set; }
        public string EstimatedDeposit { get; set; }


        public string ExpenseDetail { get; set; }
        public string DeductionsDetail { get; set; }
        public string EarnedEquity { get; set; }


        public bool ContractorPayment { get; set; }
        public bool W2Payment { get; set; }
        public bool OfficierPayment { get; set; }

        private AppUserDTO _user;
        [NotMapped]
        public AppUserDTO User
        {
            get => _user;
            set
            {
                _user = value;
                UserId = _user?.AppUserId;
            }
        }
    }
}
