using LagoVista.CloudStorage.Interfaces;
using System;

namespace LagoVista.CloudStorage.Repositories
{
    public interface IEntityListItemRepoFactory
    {
        IEntityListItemRepo Create(Type entityType);
    }
}
