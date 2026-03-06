using LagoVista.Core;
using LagoVista.Core.Product;
using LagoVista.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace LagoVista.Relational.DataContexts
{
    public class BillingDataContext : DbContext
    {
        public BillingDataContext(DbContextOptions<BillingDataContext> optionsContext) :
            base(optionsContext)
        {

        }
       
        public string GetBersion => this.GetType().Assembly.GetName().Version.ToString();

        public DbSet<AppUserDTO> AppUser { get; set; }
        public DbSet<OrganizationDTO> Org { get; set; }


        public DbSet<ProductDTO> Product { get; set; }

        public DbSet<ProductCategoryDTO> ProductCategory { get; set; }
        public DbSet<ProductIncludedDTO> ProductIncluded { get; set; }
        public DbSet<ProductPageDTO> ProductPage { get; set; }
        public DbSet<ProductPageProductDTO> ProductPageProduct { get; set; }


        public DbSet<AccountDto> Account { get; set; }
        public DbSet<AccountTransactionDto> Transaction { get; set; }
        public DbSet<TransactionStagingDto> TransactionStaging { get; set; }

        public DbSet<AccountTransactionCategoryDto> AccountTransactionCategory { get; set; }


        public DbSet<AgreementDTO> Agreements { get; set; }
        public DbSet<AgreementLineItemDTO> AgreementLineItems { get; set; }


        public DbSet<BillingEventDTO> BillingEvents { get; set; }
        public DbSet<BudgetItemDTO> BudgetItems { get; set; }


        public DbSet<CustomerDTO> Customers { get; set; }

        public DbSet<DeviceOwnerDTO> DeviceOwnerUser { get; set; }
        public DbSet<OwnedDeviceDTO> DeviceOwnerUserDevices { get; set; }


        public DbSet<ExpenseCategoryDTO> ExpenseCategory { get; set; }
        public DbSet<ExpenseDTO> Expenses { get; set; }

        public DbSet<LicenseDTO> License { get; set; }
        public DbSet<LicenseUsageDTO> LicenseUsage { get; set; }


        public DbSet<InvoiceDTO> Invoices { get; set; }
        public DbSet<InvoiceLineItemDTO> InvoiceLineItems { get; set; }
        public DbSet<InvoiceLogsDTO> InvoiceLogs { get; set; }



        public DbSet<PaymentDTO> Payments { get; set; }
        public DbSet<PayRateDTO> PayRates { get; set; }
        public DbSet<PayrollSummaryDTO> PayrollSummary { get; set; }

        public DbSet<RecurringCycleTypeDTO> RecurringCycleTypes { get; set; }

        public DbSet<SerialNumberDTO> SerialNumbers { get; set; }

        public DbSet<SubscriptionDTO> Subscription { get; set; }

        public DbSet<TimeEntryDTO> TimeEntries { get; set; }
        public DbSet<TimePeriodDTO> TimePeriods { get; set; }

        public DbSet<UnitTypeDTO> UnitTypes { get; set; }

        public DbSet<VendorDTO> Vendor { get; set; }

        public DbSet<WorkRoleDTO> WorkRoles { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.SeedProviderName(Database.ProviderName);

            AccountDto.Configure(modelBuilder);
            AccountTransactionDto.Configure(modelBuilder);

            AppUserDTOConfig.Configure(modelBuilder);

            AgreementDTO.Configure(modelBuilder);
            AgreementLineItemDTO.Configure(modelBuilder);

            AccountTransactionCategoryDto.Configure(modelBuilder);

            BillingEventDTO.Configure(modelBuilder);

            BudgetItemDTO.Configure(modelBuilder);

            CustomerDTO.Configure(modelBuilder);

            OwnedDeviceDTO.Configure(modelBuilder);
            DeviceOwnerDTO.Configure(modelBuilder);

            ExpenseDTO.Configure(modelBuilder);
            ExpenseCategoryDTO.Configure(modelBuilder);

            InvoiceDTO.Configure(modelBuilder);
            InvoiceLineItemDTO.Configure(modelBuilder);
            InvoiceLogsDTO.Configure(modelBuilder); 

            LicenseDTO.Configure(modelBuilder);
            LicenseUsageDTO.Configure(modelBuilder);

            PaymentDTO.Configure(modelBuilder);
            PayRateDTO.Configure(modelBuilder);
            PayrollSummaryDTO.Configure(modelBuilder);

            ProductCategoryDTO.Configure(modelBuilder);
            ProductDTO.Configure(modelBuilder);
            ProductIncludedDTO.Configure(modelBuilder);
            ProductPageDTO.Configure(modelBuilder);
            ProductPageProductDTO.Configure(modelBuilder);

            OrganizationDTOConfig.Configure(modelBuilder);

            modelBuilder.Entity<ProductOfferingViewDTO>(b =>
            {
                b.HasNoKey();
                b.ToView("usv_ProductOfferings");
            });

            RecurringCycleTypeDTO.Configure(modelBuilder);

            SerialNumberDTO.Configure(modelBuilder);

           SubscriptionDTO.Configure(modelBuilder);

           TimeEntryDTO.Configure(modelBuilder);
           TimePeriodDTO.Configure(modelBuilder);

            TransactionStagingDto.Configure(modelBuilder);

            VendorDTO.Configure(modelBuilder);

            UnitTypeDTO.Configure(modelBuilder);

            WorkRoleDTO.Configure(modelBuilder);

            modelBuilder.LowerCaseNames(Database.ProviderName);
            modelBuilder.ApplyUtcDateTimeConvention();
        }
    }
}
