namespace SchemaVerify.Core;

public interface ISchemaDiffer
{
    DiffReport Diff(
        string contextType,
        string provider,
        SchemaModel ef,
        SchemaModel db,
        ITypeMatcher typeMatcher);
}
