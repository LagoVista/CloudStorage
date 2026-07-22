using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models.UIMetaData;
using System;
using System.Reflection;

namespace LagoVista.CloudStorage.Managers
{
    public class EntityListResponseMetadataProvider : IEntityListResponseMetadataProvider
    {
        public void Apply<TModel>(ListResponse<TModel> response, Type entityType) where TModel : class
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            response.ModelName = entityType.Name;

            var attribute = entityType.GetTypeInfo().GetCustomAttribute<EntityDescriptionAttribute>();
            if (attribute == null)
            {
                response.Title = entityType.Name;
                return;
            }

            response.Title = GetResourceValue(attribute.ResourceType, attribute.TitleResource) ?? entityType.Name;
            response.Help = GetResourceValue(attribute.ResourceType, attribute.UserHelpResource);
            response.Icon = attribute.Icon;
            response.FactoryUrl = attribute.FactoryUrl;
            response.GetUrl = attribute.GetUrl;
            response.GetListUrl = attribute.GetListUrl;
            response.DeleteUrl = attribute.DeleteUrl;
            response.HelpUrl = attribute.HelpUrl;
        }

        private static string GetResourceValue(Type resourceType, string resourceName)
        {
            if (resourceType == null || String.IsNullOrWhiteSpace(resourceName))
                return null;

            var property = resourceType.GetTypeInfo().GetDeclaredProperty(resourceName);
            return property?.GetValue(property.DeclaringType, null) as string;
        }
    }
}
