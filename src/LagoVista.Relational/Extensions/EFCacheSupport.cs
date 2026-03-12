using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public interface ICacheScope
{
    string Key { get; }
    TimeSpan Ttl { get; }
}

public sealed record CacheScope(string Key, TimeSpan Ttl) : ICacheScope;

public sealed record CacheSpec(string Scope, TimeSpan Ttl);

public static class EfCacheScopeExtensions
{
    public static IQueryable<T> CacheScope<T>(this IQueryable<T> query, ICacheScope scope)
        => query.Provider.CreateQuery<T>(
            Expression.Call(
                instance: null,
                method: CacheScopeMethod.MakeGenericMethod(typeof(T)),
                arguments: new Expression[]
                {
                query.Expression,
                Expression.Constant(scope.Key),
                Expression.Constant(scope.Ttl)
                }));


    private static readonly MethodInfo CacheScopeMethod =
        typeof(EfCacheScopeExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(CacheScope) && m.IsGenericMethodDefinition);

    internal static bool TryStrip<T>(this IQueryable<T> query, out IQueryable<T> stripped, out CacheSpec spec)
    {
        if (query.Expression is MethodCallExpression mc &&
            mc.Method.IsGenericMethod &&
            mc.Method.GetGenericMethodDefinition() == CacheScopeMethod)
        {
            var scope = (string)((ConstantExpression)mc.Arguments[1]).Value;
            var ttl = (TimeSpan)((ConstantExpression)mc.Arguments[2]).Value;
            spec = new CacheSpec(scope, ttl);

            stripped = query.Provider.CreateQuery<T>(mc.Arguments[0]);
            return true;
        }

        stripped = query;
        spec = null;
        return false;
    }
}
