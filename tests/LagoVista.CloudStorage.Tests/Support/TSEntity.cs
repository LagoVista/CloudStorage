﻿using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests.Support
{
    public class TSEntity : TableStorageEntity
    {
        public int Index { get; set; }

        public string Value1 { get; set; }
        public string Value2 { get; set; }
    }
}
