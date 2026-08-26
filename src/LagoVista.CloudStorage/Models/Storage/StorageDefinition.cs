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
    public sealed class StorageDefinition<TEntity>
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

        public StorageDefinition<TEntity> KeyBy<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            KeyField = GetPropertyPath(selector);
            return this;
        }

        public StorageDefinition<TEntity> PartitionBy<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            AddUnique(_partitionFields, GetPropertyPath(selector));
            return this;
        }

        public StorageDefinition<TEntity> TimeBy<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            TimeField = GetPropertyPath(selector);
            return this;
        }

        public StorageDefinition<TEntity> Index<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            AddUnique(_indexedFields, GetPropertyPath(selector));
            return this;
        }

        public StorageDefinition<TEntity> BucketBy(StoragePeriod storagePeriod)
        {
            BucketPeriod = storagePeriod;
            return this;
        }

        public StorageDefinition<TEntity> RetainFor(TimeSpan retention)
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

        private static string GetPropertyPath<TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            var names = new Stack<string>();
            Expression current = UnwrapConvert(selector.Body);

            while (current is MemberExpression member && member.Member is PropertyInfo)
            {
                names.Push(member.Member.Name);
                current = UnwrapConvert(member.Expression);
            }

            if (!(current is ParameterExpression) || names.Count == 0)
            {
                throw new ArgumentException("Storage selectors must reference a property path on the entity.", nameof(selector));
            }

            return String.Join(".", names);
        }

        private static Expression UnwrapConvert(Expression expression)
        {
            while (expression is UnaryExpression unary &&
                   (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
            {
                expression = unary.Operand;
            }

            return expression;
        }
    }
}
