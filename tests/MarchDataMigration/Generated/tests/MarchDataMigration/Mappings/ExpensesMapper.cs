using MarchDataMigration.Generated.Expenses;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class ExpensesMapper
    {
        public static TargetExpensesRow Map(SourceExpensesRow source)
        {
            return new TargetExpensesRow
            {
                Id = source.Id,
                TimePeriodId = source.TimePeriodId,
                CreditCardId = default,
                ExpenseCategoryId = source.ExpenseCategoryId.Value,
                AgreementId = source.AgreementId,
                BillingEventId = source.BillingEventId,
                PaymentId = source.PaymentId,
                ExpenseDate = source.Date,
                ProjectId = source.ProjectId,
                ProjectName = source.ProjectName,
                WorkTaskId = source.WorkTaskId,
                WorkTaskName = source.WorkTaskName,
                UserId = source.UserId,
                OrganizationId = source.OrganizationId,
                Approved = source.Approved,
                ApprovedById = source.ApprovedById,
                ApprovedDate = source.ApprovedDate,
                Locked = source.Locked,
                EncryptedAmount = source.EncryptedAmount,
                EncryptedReimbursedAmount = source.EncryptedReimbursedAmount,
                Notes = source.Notes,
                Description = source.Description,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.LastUpdateDate,
                VendorId = source.VendorId,
            };
        }
    }
}
