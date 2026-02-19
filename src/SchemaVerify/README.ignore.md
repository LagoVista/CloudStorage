# Note

This file is intentionally named to discourage treating it as project documentation in your repo.

Run:
- Copy src/SchemaVerify/schema-verify.sample.json to schema-verify.json
- Update AssemblyPaths + ContextType + connection strings
- Ensure each DbContext has an IDesignTimeDbContextFactory<TContext> implementation that accepts args:
  args[0] = provider (sqlserver|postgres)
  args[1] = connection string

Then:
- dotnet run --project src/SchemaVerify
