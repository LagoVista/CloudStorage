// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 142b7c86bbdca67baf6d65528b683ab68906b55ea3d509a37ae2182c86f01c5c
// IndexVersion: 1
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage
{
    public interface ICacheProviderSettings
    {
        bool UseCache { get;  }
        IConnectionSettings CacheSettings { get; }
    }
}
