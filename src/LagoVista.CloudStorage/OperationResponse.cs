// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 4aa6ac750138e8754c6d8f339ed3e76316c4162bce4ba0c88741188933798661
// IndexVersion: 0
// --- END CODE INDEX META ---
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
