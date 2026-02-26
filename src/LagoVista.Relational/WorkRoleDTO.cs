using LagoVista.Core;
using LagoVista.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational
{
    public class WorkRoleDTO : DbModelBase
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }

        public string Icon { get; set; }
        public bool IsActive { get; set; }
    }
}
