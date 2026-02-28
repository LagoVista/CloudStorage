using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

public static class EntityAttributeGuards
{
    public static void RequireTableAttribute(Type entityType)
    {
        var attr = entityType.GetCustomAttribute<TableAttribute>();

        if (attr == null)
        {
            throw new InvalidOperationException(
                $"Entity {entityType.Name} must declare [Table(\"Name\", Schema=\"...\")] explicitly.");
        }

        if (string.IsNullOrWhiteSpace(attr.Name))
        {
            throw new InvalidOperationException(
                $"Entity {entityType.Name} has [Table] but no table name specified.");
        }

        if (string.IsNullOrWhiteSpace(attr.Schema))
        {
            throw new InvalidOperationException(
                $"Entity {entityType.Name} must explicitly define Schema in [Table].");
        }
    }
}