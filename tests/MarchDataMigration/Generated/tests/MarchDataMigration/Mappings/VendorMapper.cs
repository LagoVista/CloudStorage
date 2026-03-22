using MarchDataMigration.Generated.Vendor;

namespace MarchDataMigration.Mappings
{
    // handwritten: safe to edit, never regenerated
    public static class VendorMapper
    {
        public static TargetVendorRow Map(SourceVendorRow source)
        {
            return new TargetVendorRow
            {
                Id = source.Id,
                OrganizationId = source.OrganizationId,
                DefaultExpenseCategoryId = source.DefaultExpenseCategoryId,
                Name = source.Name,
                Key = source.Key,
                Description = source.Description,
                MaxAmount = source.MaxAmount,
                PayPeriod = source.PayPeriod,
                Notes = source.Notes,
                Contact = source.Contact,
                Phone = source.Phone,
                Icon = source.Icon,
                Address1 = source.Address1,
                Address2 = source.Address2,
                City = source.City,
                StateOrProvince = source.StateOrProvince,
                PostalCode = source.PostalCode,
                Country = source.Country,
                CreatedById = source.CreatedById,
                LastUpdatedById = source.LastUpdatedById,
                CreationDate = source.CreationDate,
                LastUpdatedDate = source.CreationDate,
                IsActive = source.IsActive,
                DefaultAccountTransactionCategoryId = source.DefaultAccountTransactionCategoryId,
            };
        }
    }
}
