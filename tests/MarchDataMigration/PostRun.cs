using System;
using System.Collections.Generic;
using System.Text;

namespace MarchDataMigration
{
    public class PostRun
    {
        string UPDATE_SQL = @"
            update ProductPageProduct set OrgnizationId = org
               from ProductCategories where 
               product.ProductCategoryId = ProductCategories.Id 

            update AgreementLineItems set CustomerId = (select CustomerId from Agreements where Agreements.Id = AgreementLineItems.AgreementId)
            update InvoiceLineItems set CustomerId = (select CustomerId from Invoices where Invoices.Id = InvoiceLineItems.InvoiceId)
            update InvoiceLogs set CustomerId = (select CustomerId from Invoices where Invoices.Id = InvoiceLogs.InvoiceId)
            update Payments set PayrollRunid= XXX

--- Enable FKey

";
    }
}
