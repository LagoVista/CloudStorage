using LagoVista.Core.Models;
using Microsoft.Data.SqlClient;
using System;

public static class SqlServerConnectionStringBuilderEx
{
    public static string Build(ConnectionSettings settings)
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

        return csb.ConnectionString;
    }
}