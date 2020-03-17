using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage
{
    public interface ICacheProvider
    {
        Task AddAsync(string key, string value);
        Task RemoveAsync(string key);
        Task<string> GetAsync(string key);
    }
}
