namespace SchemaVerify.Core;

public interface ITypeMatcher
{
    bool AreEquivalent(ColumnModel ef, ColumnModel db, out string? reason);
}

public interface ITypeMatcherFactory
{
    ITypeMatcher Create(TypeMatchMode mode, string provider);
}
