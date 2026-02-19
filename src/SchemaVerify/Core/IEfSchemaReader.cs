using Microsoft.EntityFrameworkCore;

namespace SchemaVerify.Core;

public interface IEfSchemaReader
{
    SchemaModel Read(DbContext context);
}
