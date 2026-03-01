using System;
using System.Linq;
using System.Linq.Expressions;

namespace LagoVista.Core.EF
{
    public static class EfHelpers
    {
        public static IQueryable<T> WhereEqualsIfNotEmpty<T>(this IQueryable<T> query, Expression<Func<T, string>> selector, string value)
        {
            if (string.IsNullOrEmpty(value))
                return query;

            var parameter = selector.Parameters[0];
            var body = Expression.Equal(
                selector.Body,
                Expression.Constant(value));

            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
            return query.Where(lambda);
        }

        public static IQueryable<T> WhereEqualsGuidOrThrow<T>(
               this IQueryable<T> query,
               Expression<Func<T, Guid>> selector,
               string value,
               string paramName = "value")
        {
            if (string.IsNullOrEmpty(value))
                return query;

            if (!Guid.TryParse(value, out var guid))
                throw new FormatException($"Invalid GUID for filter '{paramName}': '{value}'.");

            var p = selector.Parameters[0];
            var body = Expression.Equal(selector.Body, Expression.Constant(guid));
            var lambda = Expression.Lambda<Func<T, bool>>(body, p);

            return query.Where(lambda);
        }

        public static IQueryable<T> WhereEqualsIfNotEmpty<T>(
    this IQueryable<T> query,
    Expression<Func<T, Guid>> selector,
    string value)
        {
            if (string.IsNullOrEmpty(value))
                return query;

            if (!Guid.TryParse(value, out var guid))
                return query;

            var p = selector.Parameters[0];
            var body = Expression.Equal(selector.Body, Expression.Constant(guid));
            var lambda = Expression.Lambda<Func<T, bool>>(body, p);

            return query.Where(lambda);
        }

        public static IQueryable<T> WhereEqualsIfNotEmpty<T>(
            this IQueryable<T> query,
            Expression<Func<T, Guid?>> selector,
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return query;

            if (!Guid.TryParse(value, out var guid))
                return query;

            var p = selector.Parameters[0];
            var hasValue = Expression.Property(selector.Body, "HasValue");
            var guidValue = Expression.Property(selector.Body, "Value");
            var eq = Expression.Equal(guidValue, Expression.Constant(guid));
            var and = Expression.AndAlso(hasValue, eq);

            var lambda = Expression.Lambda<Func<T, bool>>(and, p);
            return query.Where(lambda);
        }

        public static IQueryable<T> WhereEqualsGuidOrThrow<T>(
            this IQueryable<T> query,
            Expression<Func<T, Guid?>> selector,
            string value,
            string paramName = "value")
        {
            if (string.IsNullOrEmpty(value))
                return query;

            if (!Guid.TryParse(value, out var guid))
                throw new FormatException($"Invalid GUID for filter '{paramName}': '{value}'.");

            var p = selector.Parameters[0];
            var hasValue = Expression.Property(selector.Body, "HasValue");
            var guidValue = Expression.Property(selector.Body, "Value");
            var eq = Expression.Equal(guidValue, Expression.Constant(guid));
            var and = Expression.AndAlso(hasValue, eq);

            var lambda = Expression.Lambda<Func<T, bool>>(and, p);
            return query.Where(lambda);
        }
    }
}