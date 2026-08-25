// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 4aa6ac750138e8754c6d8f339ed3e76316c4162bce4ba0c88741188933798661
// IndexVersion: 2
// --- END CODE INDEX META ---
using Microsoft.Azure.Cosmos;

namespace LagoVista.CloudStorage
{
    public class OperationResponse<TEntity>
    {
        public OperationResponse(TEntity resource)
        {
            Resource = resource;
        }

        public OperationResponse(ItemResponse<TEntity> itemResponse) : this(itemResponse.Resource)
        {
        }

        public TEntity Resource { get; }
    }
}
