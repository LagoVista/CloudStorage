using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage.Interfaces
{
    /// <summary>
    /// In vary few cases our cache might not be update,
    /// this typically happens when we are working locally
    /// and updating the database, but the cache is not exposed so
    /// we can't invalidate/update it.
    ///
    /// This will look for a query string value if if there will not check the cache.
    /// </summary>
    public interface ICacheAborter
    {
        public bool AbortCache { get; }
    }
}
