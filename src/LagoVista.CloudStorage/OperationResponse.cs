using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage
{
    public class OperationResponse<TEntity>
    {
        public OperationResponse(ItemResponse<TEntity> itemResponse)
        {
            Resource = itemResponse.Resource;
        }

        public TEntity Resource { get; }
    }
}
