using System;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityListItemRepoFactory
    {
        IEntityListItemRepo Create(Type entityType);
    }
}
