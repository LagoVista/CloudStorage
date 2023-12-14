using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Tests.Support
{
    public class DocDBEntitty : EntityBase, ISummaryFactory
    {

        public int Index { get; set; }

        public DocDbSummary CreateSummary()
        {
            return new DocDbSummary()
            {
                 Name = Name,
                 Id = Id,
                 Key = Key,
            };
        }

        ISummaryData ISummaryFactory.CreateSummary()
        {
            return this.CreateSummary();
        }
    }

    public class DocDbSummary : SummaryData
    {

    }
}
