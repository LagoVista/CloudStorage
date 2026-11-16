# D3M published-map assembler — first implementation slice

**Status:** review branch only; not integrated or deployed
**Design authority:** Data-Driven Dependency Map and Environment Promotion (same directory)

## What this adds

D3MAssemblyService reads **active published** D3M revisions from the existing
D3MDefinitionService through ID3MPublishedDefinitionReader. It never reads
a mutable draft as release truth. The existing D3MTypeRegistry and D3MValidator
remain the code-owned type/relationship allowlist and validate the published
record before any traversal.

For a selected (organization, registered root type, root ID), it:

1. Validates that the root type is registered and allowed for top-level selection.
2. Reads its published D3M revision, then asks the trusted adapter for the
   **approved/stable source snapshot**, not an arbitrary latest revision.
3. Traverses each **immediate published dependency rule**, invoking only the
   registered domain relationship accessor for the record's storage class.
4. Recurses across EntityBase, ApplicationData, Product, ProductCategory and
   Media adapters, deduplicating by organization/type/canonical record ID.
5. Keeps all reference edges. Navigation-only edges are shown but do not
   participate in prerequisite traversal or ordering.
6. Emits nodes in dependency-first order with **both** the pinned D3M revision
   and approved source-record revision.
7. Reports missing published maps, unapproved/missing snapshots, invalid type
   registrations, cross-organization identities, missing required references,
   unsupported storage adapters, cycles and bounded graph limits as blockers.

The output is **metadata only** (D3MAssemblyResult) and cannot publish,
approve, transfer, update, delete or activate source/target definitions.

## Trusted adapter contract

The domain host registers an implementation of
ID3MApprovedSourceAdapter for each supported D3MStorageKind.

Each adapter must:

- Honor the authenticated organization in D3MRecordLocator.
- Load an approved/stable source revision, returning its immutable revision
  identity. Never quietly fall back to mutable latest state.
- Return canonical record identity and exact owning organization.
- Resolve only the property path described by a **code-registered**
  D3MRegisteredRelationship; the persisted map cannot execute arbitrary
  reflection, code or queries.
- Enforce the type-specific behavior of conditional predicates and
  environment-owned bindings. A binding key is recorded in the graph and is
  **not** automatically applied.
- For a key-only reference, resolve a canonical ID using the owning domain
  handler. Ambiguous/missing identities must not silently resolve.
- For Product/ProductCategory/Media, preserve the existing typed domain
  handlers rather than using generic JSON/SQL or binary copying.

D3MAssemblyService is registered with AddD3MDefinitions; the host must
provide its approved source adapters explicitly. It is intentionally safe
to register the assembler before enabling or migrating any domain consumer.

## Scope of this PR

- Generic metadata graph assembly from published D3Ms.
- Immutable revision pins and deterministic, dependency-first traversal.
- Read-only adapter interfaces for both Mongo-backed record classes and the
  three special classes.
- Regression tests using in-memory trusted fake adapters across multiple storage
  kinds, with duplicate dependencies, required/optional/conditional references,
  navigation-only links, cycles and negative cases.

**Not included:** production domain source adapters, actual approved/stable
snapshot selection, VTM/Department D3M authoring/publishing, a signed manifest,
the DEV/LIVE planner, transfer execution, migrations or a UI cutover. The
existing hard-coded DefinitionDependencyInspector remains authoritative for
the current Definition Releases UI until a comparison-backed migration.

## Next integration cut

1. Register the existing VTM, Department, AgentContext and Media typed adapters
   in LagoVista/ai, with stable source revision readers.
2. Author and approve their immediate rules as published ApplicationData D3Ms.
   In particular, VirtualTeamMember.AgentPersonaDefinition is **optional**.
3. Run the new assembler alongside the existing inspector for the same DEV
   records and compare node/edge/blocker evidence before cutover.
4. Provide a read-only version-pinned graph endpoint and update the workbench.
   Keep live activation and writes outside this scope.
