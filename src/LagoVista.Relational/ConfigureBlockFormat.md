EF Configure House Style

1. Begin every Configure method with:
   - var mb = modelBuilder;
   - var provider = mb.GetProviderName();
   - var entity = mb.Entity<T>();

2. Use this section order:
   Relationships
   Key / indexes / concurrency
   Defaults
   Column order
   Storage types
   Provider-specific rules

3. Simple relationships must be one line.

4. Default values must use StandardDbDefaults(provider).

5. Column types must use StandardDBTypes(provider); raw SQL type strings are not allowed in DTO configuration.

6. Single-property primary keys must use HasKey(x => x.Id).

7. Column order declarations must be grouped together in their own section.

8. Multiline formatting should be reserved for special or complex configuration.

9. Provider-specific branching should be minimized and used only when helpers cannot express the behavior.

10. Property and FK naming must consistently use Id suffix casing.
