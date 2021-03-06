﻿using LagoVista.Core.Interfaces;
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
