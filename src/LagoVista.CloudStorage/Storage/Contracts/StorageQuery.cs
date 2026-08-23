using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Provider-neutral identity for mutable storage records. Scope is optional
    /// and can represent tenant/organization/logical partition where required.
    /// </summary>
    public sealed class StorageKey
    {
        public StorageKey(string id, string scope = null)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            Id = id;
            Scope = scope;
        }

        public string Id { get; }
        public string Scope { get; }
    }

    public enum StorageFilterOperator
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

    public enum StorageSortDirection
    {
        Ascending,
        Descending
    }

    public sealed class StorageFilter<TEntity>
    {
        private StorageFilter(string field, StorageFilterOperator @operator, object value)
        {
            Field = field;
            Operator = @operator;
            Value = value;
        }

        public string Field { get; }
        public StorageFilterOperator Operator { get; }
        public object Value { get; }

        public static StorageFilter<TEntity> Create<TValue>(
            Expression<Func<TEntity, TValue>> selector,
            StorageFilterOperator @operator,
            TValue value)
        {
            return new StorageFilter<TEntity>(StorageExpression.PropertyName(selector), @operator, value);
        }
    }

    public sealed class StorageSort<TEntity>
    {
        private StorageSort(string field, StorageSortDirection direction)
        {
            Field = field;
            Direction = direction;
        }

        public string Field { get; }
        public StorageSortDirection Direction { get; }

        public static StorageSort<TEntity> By<TValue>(
            Expression<Func<TEntity, TValue>> selector,
            StorageSortDirection direction = StorageSortDirection.Ascending)
        {
            return new StorageSort<TEntity>(StorageExpression.PropertyName(selector), direction);
        }
    }

    public sealed class StoragePageRequest
    {
        public StoragePageRequest(int pageSize = 100, string continuationToken = null)
        {
            if (pageSize <= 0 || pageSize > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 1000.");
            }

            PageSize = pageSize;
            ContinuationToken = continuationToken;
        }

        public int PageSize { get; }

        /// <summary>
        /// Opaque provider-owned cursor. Callers may retain and return it but
        /// must not interpret provider-specific contents.
        /// </summary>
        public string ContinuationToken { get; }
    }

    public sealed class StoragePageResult<TEntity>
    {
        public StoragePageResult(IEnumerable<TEntity> items, string continuationToken = null)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            Items = new List<TEntity>(items).AsReadOnly();
            ContinuationToken = continuationToken;
        }

        public IReadOnlyList<TEntity> Items { get; }
        public string ContinuationToken { get; }
        public bool HasMoreRecords => !String.IsNullOrWhiteSpace(ContinuationToken);
    }

    public sealed class StorageQuery<TEntity>
    {
        private readonly List<StorageFilter<TEntity>> _filters = new List<StorageFilter<TEntity>>();
        private readonly List<StorageSort<TEntity>> _sorts = new List<StorageSort<TEntity>>();

        public IReadOnlyList<StorageFilter<TEntity>> Filters => _filters;
        public IReadOnlyList<StorageSort<TEntity>> Sorts => _sorts;
        public StoragePageRequest Page { get; private set; } = new StoragePageRequest();

        public StorageQuery<TEntity> Where<TValue>(
            Expression<Func<TEntity, TValue>> selector,
            StorageFilterOperator @operator,
            TValue value)
        {
            _filters.Add(StorageFilter<TEntity>.Create(selector, @operator, value));
            return this;
        }

        public StorageQuery<TEntity> OrderBy<TValue>(
            Expression<Func<TEntity, TValue>> selector,
            StorageSortDirection direction = StorageSortDirection.Ascending)
        {
            _sorts.Add(StorageSort<TEntity>.By(selector, direction));
            return this;
        }

        public StorageQuery<TEntity> WithPage(StoragePageRequest page)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            return this;
        }
    }

    public sealed class HistoryQuery<TEntity>
    {
        private readonly List<StorageFilter<TEntity>> _filters = new List<StorageFilter<TEntity>>();

        public DateTime? StartUtc { get; private set; }
        public DateTime? EndUtc { get; private set; }
        public IReadOnlyList<StorageFilter<TEntity>> Filters => _filters;
        public StoragePageRequest Page { get; private set; } = new StoragePageRequest();

        public HistoryQuery<TEntity> Between(DateTime? startUtc, DateTime? endUtc)
        {
            if (startUtc.HasValue && endUtc.HasValue && startUtc.Value > endUtc.Value)
            {
                throw new ArgumentException("History query start time cannot be after end time.");
            }

            StartUtc = NormalizeUtc(startUtc);
            EndUtc = NormalizeUtc(endUtc);
            return this;
        }

        public HistoryQuery<TEntity> Where<TValue>(
            Expression<Func<TEntity, TValue>> selector,
            StorageFilterOperator @operator,
            TValue value)
        {
            _filters.Add(StorageFilter<TEntity>.Create(selector, @operator, value));
            return this;
        }

        public HistoryQuery<TEntity> WithPage(StoragePageRequest page)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            return this;
        }

        private static DateTime? NormalizeUtc(DateTime? value)
        {
            if (!value.HasValue) return null;
            if (value.Value.Kind == DateTimeKind.Utc) return value;
            if (value.Value.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            }

            return value.Value.ToUniversalTime();
        }
    }

    internal static class StorageExpression
    {
        public static string PropertyName<TEntity, TValue>(Expression<Func<TEntity, TValue>> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            var body = UnwrapConvert(selector.Body);
            if (body is MemberExpression member && member.Member is PropertyInfo)
            {
                var target = UnwrapConvert(member.Expression);
                if (target is ParameterExpression)
                {
                    return member.Member.Name;
                }
            }

            throw new ArgumentException("Storage selectors must reference a direct property on the entity.", nameof(selector));
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
