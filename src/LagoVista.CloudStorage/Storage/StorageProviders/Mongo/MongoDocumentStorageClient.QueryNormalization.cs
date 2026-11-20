using LagoVista;
using System;
using System.Linq.Expressions;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Mongo
{
    public sealed partial class MongoDocumentStorageClient
    {
        // Legacy repositories can compare a string document field with GuidString36.
        // The resulting expression converts the document field to GuidString36,
        // which MongoDB's LINQ provider cannot translate. Convert the captured GUID
        // to its string representation instead and keep the comparison server-side.
        internal static Expression<Func<TEntity, bool>> NormalizeGuidString36Comparisons<TEntity>(
            Expression<Func<TEntity, bool>> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return (Expression<Func<TEntity, bool>>)new GuidString36ComparisonVisitor(query.Parameters[0]).Visit(query);
        }

        private sealed class GuidString36ComparisonVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _documentParameter;

            public GuidString36ComparisonVisitor(ParameterExpression documentParameter)
            {
                _documentParameter = documentParameter;
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
                {
                    if (TryGetDocumentStringField(node.Left, out var field) &&
                        TryGetCapturedGuidString(node.Right, out var value))
                        return Expression.MakeBinary(node.NodeType, field, Expression.Constant(value));

                    if (TryGetDocumentStringField(node.Right, out field) &&
                        TryGetCapturedGuidString(node.Left, out value))
                        return Expression.MakeBinary(node.NodeType, Expression.Constant(value), field);
                }

                return base.VisitBinary(node);
            }

            private bool TryGetDocumentStringField(Expression expression, out Expression field)
            {
                field = null;

                if (!(expression is UnaryExpression conversion) ||
                    conversion.NodeType != ExpressionType.Convert ||
                    conversion.Type != typeof(GuidString36) ||
                    conversion.Operand.Type != typeof(string) ||
                    !ReferencesDocumentParameter(conversion.Operand))
                    return false;

                field = conversion.Operand;
                return true;
            }

            private bool ReferencesDocumentParameter(Expression expression)
            {
                while (expression is MemberExpression member)
                    expression = member.Expression;

                return expression == _documentParameter;
            }

            private static bool TryGetCapturedGuidString(Expression expression, out string value)
            {
                value = null;
                if (expression.Type != typeof(GuidString36) || !IsCapturedValue(expression))
                    return false;

                try
                {
                    value = Expression.Lambda<Func<GuidString36>>(expression).Compile()().Value;
                    return value != null;
                }
                catch (Exception)
                {
                    // Preserve the original expression if it cannot be evaluated safely.
                    return false;
                }
            }

            private static bool IsCapturedValue(Expression expression)
            {
                if (expression is ConstantExpression) return true;
                if (expression is MemberExpression member)
                    return member.Expression != null && IsCapturedValue(member.Expression);
                if (expression is UnaryExpression conversion && conversion.NodeType == ExpressionType.Convert)
                    return IsCapturedValue(conversion.Operand);
                return false;
            }
        }
    }
}
