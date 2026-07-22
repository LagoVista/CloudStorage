using LagoVista.Core.Models.UIMetaData;
using System;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityListResponseMetadataProvider
    {
        void Apply<TModel>(ListResponse<TModel> response, Type entityType) where TModel : class;
    }
}
