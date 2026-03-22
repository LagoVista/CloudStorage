using LagoVista.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarchDataMigration.Utils
{
    public class ConnectionStrings
    {
        public static string Build(ConnectionSettings settings, string userName = null, string password = null, string initialCatalog = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var csb = new SqlConnectionStringBuilder
            {
                DataSource = settings.Uri,                 // e.g. "localhost,1433"
                InitialCatalog = settings.ResourceName,    // e.g. "Billing"
                UserID = settings.UserName,
                Password = settings.Password,
                Encrypt = true,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
                ConnectTimeout = settings.TimeoutInSeconds > 0 ? settings.TimeoutInSeconds : 30
            };

            if(!String.IsNullOrEmpty(initialCatalog))
            {
                csb.InitialCatalog = initialCatalog;
            }   

            if (!String.IsNullOrEmpty(userName))
            {
                csb.UserID = userName;
            }

            if(!String.IsNullOrEmpty(password)) {
                csb.Password = password;
            }

            return csb.ConnectionString;

        }

    }
}
