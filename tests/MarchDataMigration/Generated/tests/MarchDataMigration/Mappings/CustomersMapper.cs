using MarchDataMigration.Generated.Customers;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class CustomersMapper
    {
        public static TargetCustomersRow Map(SourceCustomersRow source)
        {
            return new TargetCustomersRow
            {
                Id = source.Id,
                OrganizationId = source.OrganizationId,
                CustomerName = source.CustomerName,
                BillingContactName = source.BillingContactName,
                BillingContactEmail = source.BillingContactEmail,
                Address1 = source.Address1,
                Address2 = source.Address2,
                City = source.City,
                State = source.State,
                Zip = source.Zip,
                Notes = source.Notes,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = default,
            };
        }
    }
}
