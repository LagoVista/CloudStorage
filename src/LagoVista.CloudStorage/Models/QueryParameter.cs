using System;
using System.Collections.Generic;
using System.Text;

namespace LagoVista.CloudStorage
{
    public class QueryParameter
    {
        public QueryParameter(string name, Object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get;}    
        public Object Value { get;}
    }
}
