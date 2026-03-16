using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational.Helpers
{
    internal class EncryptionKeyHelpers
    {
        /*
         * account-<AccountId32hex>:v2
customer-<CustomerId32hex>:v2
org-<OrgId32hex>:v2
orguser-<OrgId32hex><UserId32hex>:v2 
        
         
         C# DTO / “Table”	Encrypted fields
AccountDto	EncryptedBalance, EncryptedOnlineBalance, EncryptedAccountNumber, EncryptedRoutingNumber
AccountTransactionDto	EncryptedAmount
AgreementDTO	EncryptedRate, EncryptedSubTotal, EncryptedDiscountPercent, EncryptedTax, EncryptedShipping, EncryptedTotal
AgreementLineItemDTO	EncryptedUnitPrice, EncryptedDiscountPercent, EncryptedExtended, EncryptedSubTotal, EncryptedShipping
InvoiceDTO	EncryptedTotal, EncryptedDiscount, EncryptedExtended, EncryptedTotalPaid,  EncryptedShipping, EncryptedSubtotal
InvoiceLineItemDTO	EncryptedUnitPrice, EncryptedTotal, EncryptedDiscount, EncryptedExtended, EncryptedShipping
InvoiceLogDTO	EncryptedAmount
PaymentDTO	EncryptedGross, EncryptedNet, EncryptedExpenses, EncryptedPrimaryDeposit, EncryptedSecondaryDeposit, EncryptedEarnedEquity
PayRate	EncryptedSalary, EncryptedDeductions, EncryptedEquityScaler, EncryptedBillableRate, EncryptedInternalRate
PayrollRun	EncryptedTotalSalary, EncryptedTotalPayroll, EncryptedTotalExpenses, EncryptedTotalPayrollTaxObligation, EncryptedTotalRevenue, EncryptedTaxLiabilities
ExpenseDTO	EncryptedAmount, EncryptedReimbursedAmount
         
         
         */

    }
}
