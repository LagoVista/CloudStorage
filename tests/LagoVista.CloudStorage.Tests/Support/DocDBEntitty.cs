﻿using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Tests.Support
{
    public class DocDBEntitty : EntityBase, ISummaryFactory, ICategorized
    {

        public int Index { get; set; }

        public string FieldOne { get; set; }

        public string FieldTwo { get; set; }

        public string FieldThree { get; set; }

        public string FieldFour { get; set; }

        public string FieldFive { get; set; }

        public string FieldSix { get; set; }

        public EntityHeader Category { get; set; }

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
        public string FieldTwo { get; set; }
        public string FieldThree { get; set; }
    }
}
