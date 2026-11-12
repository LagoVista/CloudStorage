# Data-Driven Dependency Map (D3M) and Environment Promotion

**Status:** Architectural plan / design baseline — not an implementation claim  
**Updated:** 2026-11-12  
**Home:** LagoVista/CloudStorage  
**Initial use case:** VTM and its transitive dependencies  
**Direction for the formal promotion workflow:** **LIVE = Target / Initiator**, **DEV = Source / Server**

> This document captures the agreed architecture for interactive D3M authoring, dependency assembly, cross-environment planning, and eventual promotion. Contracts and endpoints below are proposed, not yet shipped. The separate, temporary LIVE-to-DEV bootstrap/back-fill capability does not change the direction of formal DEV-to-LIVE promotion.

## 1. Why we need this

Much of AppTrex/APTIX behavior is driven by definitions persisted as data. The runtime behavior of a promoted VirtualTeamMember (VTM) depends on other independently owned definitions, SQL catalog data, and media resources. Promotion is therefore a **first-class data lifecycle operation**, not an untyped document copy or a single-record JSON upsert.

We want to:

- Choose one **top-level promotable record** through a UI, beginning with VTM.
- Assemble the **complete dependency closure**, from its immediate relationships through the relationships of all descendants.
- Use an **approved/stable version** of each participating source record; do not accidentally ship an unapproved working version.
- Let **LIVE** inspect a metadata-only manifest and authoritatively decide what is already available versus what needs transfer, rebinding, or review.
- Import through the correct domain-specific manager/repository instead of bypassing business semantics with generic JSON upserts.
- Track promotion provenance and support safe reruns and eventual JobManager execution.
- Leave the door open to reusable, versioned behavioral packages later, **without** adopting a package manager in V1.

## 2. Terminology and responsibility

| Term | V1 meaning |
| --- | --- |
| **LIVE** | **Target and initiator**. A LIVE operator/UI initiates promotion, asks DEV for manifests/data, decides the target Sync Plan, approves and applies it. |
| **DEV** | **Source and server**. DEV exposes authorized signed read operations to provide its approved/stable source versions, graph inventory, and selected record payloads. |
| **D3M** | **Data-Driven Dependency Map**: a persistent ApplicationData definition for one record type, containing **only its immediate dependency rules**. |
| **D3M Assembler** | Recursively follows those immediate rules at runtime, loading each referenced record's own D3M if one exists. |
| **Promotion Manifest** | Metadata-only, resolved graph for a specific root and specific stable source versions: nodes, edges, type/handler classification, and blockers. |
| **Sync Plan** | Target-authored decisions for each manifest node: reuse, transfer, rebind/review, or block. Must be immutable/version-bound once approved. |
| **Promotion Execution** | Durable work applying an approved plan, including typed records, media copy, verification, provenance, and indexing. |
| **Stable version** | A source revision independently marked approved/ready for promotion. It is **not necessarily** the mutable latest document version. |
| **Root promotable type** | A type operators may select directly. Its dependencies can be non-root types and are still eligible to participate transitively. |

These terms are intentional. **LIVE initiates** the promotion even though **DEV supplies** most of the content.

## 3. D3M is durable ApplicationData

D3Ms themselves are ApplicationData records, following the current CloudStorage \`IApplicationDataRecord\` conventions (organization-scoped identity, timestamps, and the shared \`IApplicationDataStore\`). CloudStorage owns the storage mechanics. A dedicated D3M repository/service owns lookups, validation, versioning, and publication.

**One primary D3M record per participating CLR/domain record type**, within the appropriate authoritative organizational scope. A D3M is a controlled published definition, not an ad hoc JSON fragment attached to every entity instance.

Each definition should identify:

- Stable record-type identity (registered domain/CLR type; no caller-chosen arbitrary assembly/type activation).
- Friendly name and description.
- **Storage/transfer classification**, as described in §4.
- An ordered list of **direct** dependency rules.
- Required/optional/conditional participation rules.
- Whether each property is a single reference or collection, and how to extract stable referenced IDs/keys.
- If needed, an explicit promotion binding/compatibility rule (such as exact stable version versus target-stable-compatible).
- D3M schema/version, publication/approval metadata, and an enable/retired state.
- Whether the type is allowed as a **top-level selection** or only reachable as a dependency.
- Whether a type is an **explicit leaf**: no additional declared dependencies.

**No transitive knowledge in the parent map.** For example:

- VTM D3M says \`DefaultAgentContext -> AgentContext\`.
- AgentContext D3M says \`ToolBoxes[] -> AgentToolBox\`.
- AgentToolBox D3M says \`ActiveTools[] -> AgentTool\`.
- VTM does **not** contain the AgentToolBox rule and does not need to know an AgentContext D3M exists until assembly time.

Do not silently interpret a missing D3M as a leaf. If a referenced type is known/registered for promotion but has no published D3M or explicit-leaf declaration, the assembler **blocks** the manifest. This avoids unknowingly omitting behavioral dependencies.

### 3.1 Initial storage/transfer classifications

Keep the dispatcher intentionally small and finite:

| Classification | Handling |
| --- | --- |
| **EntityBase** | First-class entity document; use its specific typed domain manager/repository and MongoDB storage. |
| **ApplicationData** | Durable ApplicationData record; include its **registered collection/type identity** and use the type-specific repository with \`IApplicationDataStore\` (MongoDB). |
| **Product** | **Hard-coded special case**; SQL Server-backed product logic, business-specific typed API, catalog identity mapping. |
| **ProductCategory** | **Hard-coded special case**; SQL Server-backed category logic and its Category Type prerequisite. |
| **Media** | **Hard-coded special case**; Media Resource metadata and separately transferred SeaweedFS bytes. |

**ProductCategoryType** is a prerequisite of ProductCategory. For V1, resolve it within the registered **ProductCategory special-case domain handler**, rather than inventing an unapproved fourth general dispatch classification. Confirm its precise typed contract and handling when its D3M is authored. No raw SQL Server imports.

This classification belongs on the D3M record; the implementation behind each classification uses a fixed registered adapter/handler. A D3M cannot inject executable C#, arbitrary queries, or storage connection information.

### 3.2 Proposed dependency rule attributes

Start small; preserve room to grow.

| Attribute | Meaning | V1 treatment |
| --- | --- | --- |
| Property/path | Registered typed relationship accessor, e.g., \`Department\`, \`ToolBoxes\`, \`EditorialImages\` | Required |
| Target record type | Registered destination type | Required |
| Cardinality | One or many | Required |
| Presence policy | Required, optional, conditional | Required |
| Relationship role | Prerequisite versus reverse/navigation-only reference | Required so reverse links cannot force cycles |
| Version compatibility | Exact approved source snapshot or acceptable pre-existing stable target revision | Support where needed; conservative default |
| Environment binding | Requires receiver-local resolution rather than transferring environment-specific configuration | Flag/block for explicit handler |
| Transfer ordering | Target before dependent; handler-specific exceptions only | Deterministic |

"Optional" means **the reference may be absent**. If an optional reference **is populated**, it must still resolve to a valid acceptable stable dependency or be explicitly handled; it is not permission to silently drop content. "Conditional" requires a validated, known predicate with predictable results.

## 4. Interactive authoring and MCP publication

D3M construction is intentionally a **human + assistant guided workflow**, not automatic reflection-based production of all dependencies.

1. Select a candidate record type (begin with VTM).
2. Inspect its actual source model, managers/repos, property types, and existing dependency-inspection logic.
3. Together identify **only direct** references, including nested collections and those requiring destination bindings.
4. Classify each relationship and identify the record's storage/transfer classification.
5. Validate examples, absent optional properties, required fields, reverse references, and potential cycles.
6. **Publish the D3M via MCP** to its authoritative ApplicationData store.
7. Record publication identity and version so a promotion manifest can pin the exact definition used.

The publication MCP surface should expose **review/draft, validate, publish, read/list, and revision history**. Specific MCP tool names, interfaces, and provisioning details are deferred; do not imply those endpoints already exist. Publication must reject unknown type/property registrations, invalid conditional rules, graph-schema errors, and changes that invalidate existing approved manifests without versioning.

This is a bounded set of reasonably small definitions. We will author them incrementally with source inspection, rather than building a large automatic mapping subsystem first.

## 5. Recursive D3M assembly

The assembler is generic orchestration with a registered per-class record reader/resolver. It does not encode the complete dependency tree in VTM code.

Given root \`(VirtualTeamMember, id)\`:

1. Resolve the root's **approved/stable snapshot**, registration, and published D3M.
2. Extract its immediate references according to its D3M.
3. For each referenced record, resolve its identity and an acceptable **approved/stable revision**; load that record's published D3M and repeat.
4. Deduplicate by **record-type identity + stable record identity + relevant organization/source scope**. Preserve *all incoming edges*. Do not emit one AgentContext for each of five VTMs referring to it.
5. Detect and report circularity. Ignore declared reverse/navigation-only links for prerequisite ordering. A genuine required cycle is a blocker unless handlers explicitly support a safe staged-link strategy.
6. Pin D3M versions and source record versions; generate a dependency-first/topological execution order wherever the graph permits it.
7. Produce a **metadata-only manifest** with a unique ID, root, graph version, node/edge set, stable source versions, readiness, destination-binding requirements, and blockers.

Example closure:

\`\`\`text
VTM
  -> Department
  -> Agent Context
       -> Agent Toolboxes
            -> Agent Tools
  -> Agent Persona (if present)
  -> Media (avatar/editorial, if present)
  -> Product
       -> ProductCategory
            -> ProductCategoryType
\`\`\`

The diagram shows illustrative edges, **not a substitute for inspecting the concrete record types**. Some VTMs also refer to capabilities, EJAs, workflow definitions, and other definitions; these will be identified while authoring each D3M.

**Shared dependencies appear once in the node set** even when many parents reference them. The manifest retains edges so the UI can explain why a node is needed.

## 6. Readiness and version-aware reuse

**No unapproved source record version may be transferred.** This applies anywhere in the closure, including optional-but-populated dependencies.

A source's latest record revision may be newer than its latest stable revision. A dependency does **not** necessarily block the entire promotion if LIVE already has a compatible, independently acceptable stable version:

| Source state | Target state | Proposed decision |
| --- | --- | --- |
| DEV Department V12 **not approved**; no approved V12 payload | LIVE Department V11 **stable**, compatible with requesting VTM | **Reuse LIVE V11**; do not transfer DEV V12 |
| DEV Department V12 **not approved** | LIVE missing the Department | **Blocked**, unless DEV retains an approved compatible earlier snapshot |
| DEV Department V12 **approved** | LIVE has V11 | **Transfer V12** if policy and validations permit |
| DEV root VTM not ready | Any | **Do not promote that root** |
| Both have same verified stable version and identity | Target meets constraints | **Already current** |

Acceptance of an older LIVE dependency must be explicit and compatible: a requesting VTM might require a specific revision or feature that V11 does not have. **Version number alone is not a compatibility proof.**

Track a *source version* separate from the target's local document version and store promotion provenance. The first cut can use document/stable version plus last-promotion metadata as its comparison basis; do **not** depend on raw whole-object serialization hashes. If independent histories diverge, do not assume the higher numeric version wins. A version must correspond to a reliably retrievable immutable approved snapshot; if that facility does not yet exist, surface a blocker rather than reading unapproved latest data.

For media **bytes**, keep cryptographic SHA-256 and byte-length verification. This protects the transferred payload; it is not a business-document equivalence mechanism.

## 7. Formal LIVE-initiated promotion protocol

This is the canonical sequence:

1. **LIVE UI chooses top-level record** and its type, then initiates a promotion request to DEV.
2. **DEV assembles the complete D3M dependency closure** and the current *stable/approved* source versions. DEV returns the **metadata-only manifest**.
3. **LIVE receives the manifest in one request** and checks its own MongoDB/ApplicationData, SQL catalog, and Media inventory. LIVE authors a version-pinned **Sync Plan** with one decision per node.
4. **LIVE displays the plan** and the reason for each choice. The operator approves an exact manifest + plan revision; blocked items are resolved or the plan is rejected.
5. **DEV assembles only the requested, approved typed records** for the approved plan and returns the import payload to LIVE. The package excludes bulk media bytes.
6. **LIVE applies** records through the correct typed handlers **from dependency leaves upward**, resolving ownership, environment-local values, relational mappings, and concurrency. Save durable per-node status and verify imports.
7. **LIVE moves media one resource at a time** in separate signed operations, verifies bytes/metadata, and links the resources at the correct stage of applying dependent records. Media references cannot be considered complete until their bytes are verified.
8. **LIVE finalizes** the record/graph promotion, marks managed LIVE definitions read-only for ordinary edits, records source stable version and promotion history, and enqueues the completed definitions for VectorDB indexing.
9. **LIVE reports** applied/reused/blocked/failed/index-pending nodes. Rerun only incomplete actions, with idempotency and version checks.

In short:

\`\`\`text
LIVE Target / UI                   DEV Source / Server
  select type + ID  -------------->  resolve stable graph from published D3Ms
  receive manifest <--------------  metadata-only full closure
  build target Sync Plan
  operator reviews/approves
  request approved nodes -------->  return typed stable source records
  typed import + media transfers    serve individually requested media
  verify, finalize, index
\`\`\`

**LIVE owns the plan** because LIVE knows the destination's actual inventory, stable revisions, identity mappings, and allowable changes. DEV never chooses LIVE ownership or secrets.

### 7.1 Sync Plan decisions

The target must distinguish:

- **Already current** — same compatible stable revision already applied.
- **Reuse existing** — acceptable stable target version, even if the source has a newer *unapproved* latest revision.
- **Transfer** — stable approved source payload required.
- **Rebind/review** — source identity differs, SQL catalog mapping or destination-local connection/binding is required.
- **Blocked** — absent required record, incompatible version, unapproved source version, unknown handler/D3M, unresolvable cycle, or other failed preflight.
- **Media transfer pending** — resource metadata exists but content has not been verified.

The receiver should be able to explain *which path through the graph* makes each record necessary and the stable version used to satisfy it.

### 7.2 Package is a transport concept, not a package-manager feature

V1 may ship a version-pinned record payload for the selected Sync Plan; it is **not** a reusable dependency package with NuGet/NPM-style resolution or a package registry.

Future package semantics are attractive because application definitions affect runtime behavior. Defer package naming, version resolution across product releases, package feeds, install/uninstall, and lockfiles. The D3M graph and deduplicated manifest make that future possible without imposing it now.

## 8. Media, SQL, Mongo, and indexing boundaries

**V1 supported stores:** MongoDB \`EntityBase\` records and \`ApplicationData\` records; SeaweedFS media with MongoDB Media Resource metadata; SQL Server **Product/ProductCategory special cases** and their required category/type catalog data.

- **EntityBase:** ordinary typed entity managers/repositories must validate owner remapping, invariants, references, approved version, and read-only semantics. Do not bypass these rules via generic raw JSON upserts.
- **ApplicationData:** use the specific registered record type and collection via the domain repository; CloudStorage supplies \`IApplicationDataStore\`. Respect version/concurrency token distinctions.
- **Product / ProductCategory:** dedicated SQL Server-aware handlers. Category Type is a dependency; resolve its target mapping and ensure categories exist before Products. No generic direct SQL insert.
- **Media:** manifest records reference Media Resource IDs/revisions and expected content fingerprint/size. **No image/video bytes in the record package.** Transfer each resource in a separate deterministic, resumable, signed operation; verify SHA-256, length, and destination accessibility, and update target-owner metadata.
- **VectorDB:** after successful LIVE application, publish/index the relevant records. Indexing is a post-commit, retryable step; an indexing failure must not cause unbounded reimport or pretend the promotion fully completed.

A transfer should not accidentally import environment-bound secrets, credential-bearing Agent Context fields, runtime endpoints, connection data, or source-owner IDs. Domain handlers explicitly preserve/resolve receiver-local values.

## 9. Lifecycle, ownership, and security

- Promotion is a **formal managed lifecycle**: DEV authors approved versions; LIVE consumes them through signed, reviewed plans. LIVE promoted definitions become **read-only to normal editing**, enforced at the manager/repository boundary, not just in the UI.
- The authorized LIVE promotion importer is the controlled exception allowed to update those records and their promotion metadata.
- Record audit and promotion history should retain \`PromotionId\`, source environment, source stable version, D3M version, approval actor/date, import date, and result; the precise placement of embedded metadata versus durable ledger entries remains a design choice. Avoid unnecessary writes to domain content just for tracking.
- Authentication uses the existing signed service-request framework, with explicit trusted AppKey + origin environment bindings and separate read/write authorization. Neither endpoint accepts an arbitrary caller-specified destination organization. LIVE uses its own configured LIVE user/organization identity for writes.
- An approved plan must bind exact manifest version, source record versions, graph definition versions, and root identity. Changes between planning and execution invalidate affected plan entries.
- Promotion operations need durable progress and idempotency: same plan/operation ID cannot import a record twice or replay an unapproved newer version.
- Temporary **LIVE -> DEV back-fill** is a separate bootstrap path controlled by DEV's \`EnableBackfillImport\`; do not use that exception as the future LIVE promotion authorization policy.

## 10. Existing building blocks and responsibility split

Potential pieces to reuse, subject to detailed integration review:

- \`LagoVista/WebCommon\`: existing \`SyncController\` for comparison/lookup and signed \`ServiceSyncController\`; the service sync writer currently restricts writes to Modules and is **not** the new generic promotion importer.
- \`LagoVista/CloudStorage\`: \`IApplicationDataStore\` for D3M definitions; \`IDocumentStorageClient\`/entity storage and \`SyncRepository\` for low-level access, not as a substitute for typed promotion handlers.
- \`LagoVista/ai\`: \`DefinitionDependencyInspector\`, Definition Release inventory/comparison and existing media fingerprint/planning logic are helpful **source material** for D3M authoring, not parallel truth once D3Ms are published.
- Domain-specific AI, Billing, and Media managers/repos: responsible for authoritatively loading/validating/applying typed records.

**Generic D3M assembler + manifest/protocol + durable orchestrator** should remain independent from **concrete per-type storage and business handlers**. Domain-specific C# stays acceptable where needed (property accessors, conditional logic, SQL mapping, environment rebinding) while **the list of relationships is published data**.

## 11. V1 implementation slices

1. **D3M model + validation + MCP publication:** ApplicationData record, type registry, controlled authoring, version/revision management, required/optional/collection rules. No import mutation yet.
2. **First human-reviewed D3Ms:** inspect VTM immediate relationships, then Department, AgentContext, AgentToolBox, AgentPersona, Media, Product, ProductCategory and required leaves. Publish each definition individually via MCP.
3. **Read-only assembler:** given a VTM and source organization, produce a deduplicated, version-pinned, approval-aware graph; test cycles, reused dependencies, missing mappings and unapproved latest versions.
4. **DEV manifest service + LIVE planner:** signed metadata-only exchange, LIVE inventory/compatibility checks, UI plan and blocker explanations. No writes yet.
5. **Typed import and media copy:** dependency-ordered Mongo/ApplicationData and SQL special handlers, per-resource SeaweedFS transfer, idempotent retry, destination ownership and read-only controls.
6. **Finalize and operate:** durable audit/provenance, VectorDB indexing after commit, health/status UI. Later, expose the same orchestration through JobManager with explicit approval policy.

Do not create a separate general package manager as part of these slices. The initial UI is operator-driven; JobManager is a future invocation mechanism for the **same** underlying plan/execution API.

## 12. Decisions to validate during the first D3M sessions

These are intentionally left for implementation design rather than silently asserted as solved:

- Where **stable/approved snapshot** content is retained for mutable EntityBase and ApplicationData records, and whether all types support retrieving a prior approved revision.
- How to express **compatible older LIVE versions** and required minimum/feature constraints without introducing a full package solver.
- Exact D3M publication authority, organizational scope, and how publication is mirrored/available across DEV/LIVE.
- ProductCategoryType's concrete handler/API and target identity mapping under the ProductCategory special case.
- Cycles that are *real* prerequisites versus reverse references; whether any supported class requires a two-phase link strategy.
- Precisely which top-level types are allowed for operator selection, and how read-only is enforced for those records in LIVE.
- Version provenance fields and durable promotion ledger models, including retention/rollback behavior.
- Signed endpoint shapes and batch size limits. **One manifest call** does not require one unbounded media/content transfer.

### First practical working session

Inspect \`VirtualTeamMember\` in its actual C# model; identify immediate dependencies; classify each as required, optional, or conditional and single or collection; mark its storage type; review the resulting **one-level** D3M; and publish it via MCP. Only then repeat for \`AgentContext\`, \`Department\`, and the other types discovered by assembly.

The goal is **one source of truth for each type's immediate dependencies** and **one deterministic, deduplicated graph** for each promotion.