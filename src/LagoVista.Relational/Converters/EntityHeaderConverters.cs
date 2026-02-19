using LagoVista.Core.Interfaces.AutoMapper;
using LagoVista.Core.Models;
using LagoVista.IoT.Billing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational.Converters
{
    public sealed class EntityHeaderDtoConverter : IMapValueConverter
    {
        public bool CanConvert(Type sourceType, Type targetType)
        {
            var st = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
            var tt = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (tt != typeof(EntityHeader))
                return false;

            return st == typeof(AppUserDTO) || st == typeof(OrganizationDTO);
        }

        public object Convert(object sourceValue, Type targetType)
        {
            if (sourceValue == null)
                return null;

            if (sourceValue is AppUserDTO user)
                return user.ToEntityHeader();

            if (sourceValue is OrganizationDTO org)
                return org.ToEntityHeader();

            throw new InvalidOperationException($"Unsupported conversion from {sourceValue.GetType().Name} to EntityHeader.");
        }
    }

}
