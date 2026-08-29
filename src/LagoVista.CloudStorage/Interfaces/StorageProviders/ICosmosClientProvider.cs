using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface ICosmosClientProvider
    {
        CosmosClient GetClient(string uri, string accessKey);
    }
}
