using MongoDB.Driver;
using System;
using System.Linq.Expressions;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Mongo
{
    internal static class MongoSortDefinitionBuilderExtensions
    {
        public static SortDefinition<TEntity> Ascending<TEntity, TKey>(
            this SortDefinitionBuilder<TEntity> builder,
            Expression<Func<TEntity, TKey>> expression)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            return builder.Ascending(new ExpressionFieldDefinition<TEntity, TKey>(expression));
        }

        public static SortDefinition<TEntity> Descending<TEntity, TKey>(
            this SortDefinitionBuilder<TEntity> builder,
            Expression<Func<TEntity, TKey>> expression)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            return builder.Descending(new ExpressionFieldDefinition<TEntity, TKey>(expression));
        }
    }
}
