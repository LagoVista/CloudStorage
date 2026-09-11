using LagoVista.Core.Models.UIMetaData;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LagoVista.CloudStorage.Storage
{
    internal static class ListRequestSortResolver
    {
        public static PropertyInfo ResolveProperty<TEntity>(ListRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrWhiteSpace(request.SortField)) return null;

            var property = typeof(TEntity)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate =>
                    candidate.CanRead &&
                    candidate.GetIndexParameters().Length == 0 &&
                    String.Equals(candidate.Name, request.SortField, StringComparison.OrdinalIgnoreCase));

            if (property == null)
                throw new ArgumentException($"Sort field '{request.SortField}' is not a readable public property on {typeof(TEntity).Name}.", nameof(request));

            if (!IsSupportedScalar(property.PropertyType))
                throw new ArgumentException($"Sort field '{request.SortField}' on {typeof(TEntity).Name} is not a supported scalar sort type.", nameof(request));

            return property;
        }

        public static IQueryable<TEntity> Apply<TEntity>(IQueryable<TEntity> query, ListRequest request)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var property = ResolveProperty<TEntity>(request);
            if (property == null) return query;

            var descending = request.SortDescending == true;
            var ordered = ApplyOrder(query, property, descending, thenBy: false);

            var idProperty = typeof(TEntity)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate =>
                    candidate.CanRead &&
                    candidate.GetIndexParameters().Length == 0 &&
                    String.Equals(candidate.Name, "Id", StringComparison.OrdinalIgnoreCase) &&
                    IsSupportedScalar(candidate.PropertyType));

            if (idProperty != null && !String.Equals(idProperty.Name, property.Name, StringComparison.OrdinalIgnoreCase))
                ordered = ApplyOrder(ordered, idProperty, descending, thenBy: true);

            return ordered;
        }

        private static IOrderedQueryable<TEntity> ApplyOrder<TEntity>(IQueryable<TEntity> query, PropertyInfo property, bool descending, bool thenBy)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "item");
            var body = Expression.Property(parameter, property);
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(TEntity), property.PropertyType);
            var selector = Expression.Lambda(delegateType, body, parameter);

            var methodName = thenBy
                ? (descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy))
                : (descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy));

            var method = typeof(Queryable).GetMethods()
                .Single(candidate => candidate.Name == methodName && candidate.IsGenericMethodDefinition && candidate.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TEntity), property.PropertyType);

            return (IOrderedQueryable<TEntity>)method.Invoke(null, new object[] { query, selector });
        }

        private static bool IsSupportedScalar(Type type)
        {
            var scalar = Nullable.GetUnderlyingType(type) ?? type;
            return scalar.IsEnum ||
                   scalar == typeof(string) ||
                   scalar == typeof(bool) ||
                   scalar == typeof(byte) ||
                   scalar == typeof(sbyte) ||
                   scalar == typeof(short) ||
                   scalar == typeof(ushort) ||
                   scalar == typeof(int) ||
                   scalar == typeof(uint) ||
                   scalar == typeof(long) ||
                   scalar == typeof(ulong) ||
                   scalar == typeof(float) ||
                   scalar == typeof(double) ||
                   scalar == typeof(decimal) ||
                   scalar == typeof(Guid) ||
                   scalar == typeof(DateTime) ||
                   scalar == typeof(DateTimeOffset) ||
                   scalar == typeof(DateOnly) ||
                   scalar == typeof(TimeOnly);
        }
    }
}
