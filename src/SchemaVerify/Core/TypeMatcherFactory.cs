namespace SchemaVerify.Core;

public sealed class TypeMatcherFactory : ITypeMatcherFactory
{
    public ITypeMatcher Create(TypeMatchMode mode, string provider)
    {
        provider = (provider ?? string.Empty).Trim().ToLowerInvariant();

        return mode switch
        {
            TypeMatchMode.Strict => new StrictTypeMatcher(),
            TypeMatchMode.Family => new FamilyTypeMatcher(provider),
            _ => new FamilyTypeMatcher(provider)
        };
    }
}
