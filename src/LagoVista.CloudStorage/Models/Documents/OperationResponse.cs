// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 4aa6ac750138e8754c6d8b58d8f05580b11b5538f85a70b83a071b1d446265b65
// IndexVersion: 2
// --- END CODE INDEX META ---

namespace LagoVista.CloudStorage
{
    public class OperationResponse<TEntity>
    {
        public OperationResponse(TEntity resource)
        {
            Resource = resource;
        }

        public TEntity Resource { get; }
    }
}
