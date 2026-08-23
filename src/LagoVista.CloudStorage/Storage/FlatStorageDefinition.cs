using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Describes the logical storage shape required by a repository without
    /// coupling the repository to a specific physical backend.
    /// </summary>
    public sealed class FlatStorageDefinition<TEntity>
    {
        private readonly List<string> _partitionFields = new List<string>();
        private readonly List<string> _indexedFields = new List<string>();

        public IReadOnlyList<string> PartitionFields => _partitionFields;
        public IReadOnlyList<string> IndexedFields => _indexedFields;

        public string KeyField { get; private set; }
        public string TimeField { get; private set; }

        /// <summary>
        /// Logical time bucketing hint. Azure Table Storage may translate this
        /// into physical table names while Cassandra can translate it into a
        /// partition component.
        /// </summary>
        public StoragePeriod BucketPeriod { get; private set; } = StoragePeriod.All;

        public TimeSpan? Retention { get; private set; }

        public FlatStorageDefinition<TEntity> KeyBy<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            KeyField = GetPropertyName(selector);
            return this;
        }

        public FlatStorageDefinition<TEntity> PartitionBy<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            AddUnique(_partitionFields, GetPropertyName(selector));
            return this;
        }

        public FlatStorageDefinition<TEntity> TimeBy<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            TimeField = GetPropertyName(selector);
            return this;
        }

        public FlatStorageDefinition<TEntity> Index<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            AddUnique(_indexedFields, GetPropertyName(selector));
            return this;
        }

        public FlatStorageDefinition<TEntity> BucketBy(StoragePeriod storagePeriod)
        {
            BucketPeriod = storagePeriod;
            return this;
        }

        public FlatStorageDefinition<TEntity> RetainFor(TimeSpan retention)
        {
            if (retention <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(retention), "Retention must be greater than zero.");
            }

            Retention = retention;
            return this;
        }

        private static void AddUnique(List<string> fields, string propertyName)
        {
            if (!fields.Contains(propertyName))
            {
                fields.Add(propertyName);
            }
        }

        private static string GetPropertyName<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            Expression body = selector.Body;
            if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                body = unary.Operand;
            }

            if (!(body is MemberExpression member) || !(member.Member is PropertyInfo))
            {
                throw new ArgumentException("Storage selectors must reference a direct property on the entity.", nameof(selector));
            }

            if (member.Expression is ParameterExpression)
            {
                return member.Member.Name;
            }

            throw new ArgumentException("Storage selectors must reference a direct property on the entity.", nameof(selector));
        }
    }
}
