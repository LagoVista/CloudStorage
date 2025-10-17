// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 2b5c24f0045921a77a82f8e57ca3513db24af95bb423a7980d8a721824da03ab
// IndexVersion: 1
// --- END CODE INDEX META ---
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

        public override string ToString()
        {
            return $"{Name} = {Value}"; 
        }
    }
}
