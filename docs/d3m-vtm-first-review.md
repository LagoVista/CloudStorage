# VTM D3M — First One-Level Review Candidate

**Status:** Source-inspected draft for human + assistant review; **not published**, not installed in DEV/LIVE.  
**Source:** `LagoVista/ai/src/LagoVista.AI.Models/VirtualTeamMember.cs` and `LagoVista/ai/src/LagoVista.AI/Services/DefinitionRelease/DefinitionDependencyInspector.cs` (2026-11-12).  
**Owner:** `VirtualTeamMember` (EntityBase). **Top-level selectable:** yes. **Explicit leaf:** no.

This is the initial **direct** dependency set. Each target's own dependencies belong only to its own D3M, never to this map. Presence flags below are proposed from form fields and the existing inspector, not yet authoritative promotion policy.

## Strong initial candidates

| VTM property path | Target type | Cardinality | Proposed presence | Notes |
| --- | --- | --- | --- | --- |
| `Department` | `VirtualTeamDepartment` | One | Required | Form requires it; source inspector treats as required |
| `AgentPersonaDefinition` | `AgentPersonaDefinition` | One | Required | Form requires it; source inspector treats as required |
| `DefaultAgentContext` | `AgentContext` | One | Conditional | Optional on form; blocking readiness review expects operational context; environment binding needed |
| `AvatarImage` | `MediaResource` | One | Optional | Transfer media bytes separately |
| `EditorialImages` | `MediaResource` | Many | Optional | All populated resources required in closure |
| `PromptTokenProduct` | `Product` | One | Optional | SQL special-case handler |
| `CompletionTokenProduct` | `Product` | One | Optional | SQL special-case handler |
| `BusinessCapabilities` | `BusinessCapability` | Many | Optional | Source inspector includes it |
| `EssentialJobActivities` | `EssentialJobActivity` | Many | Conditional | Form optional but blocking readiness criteria expect coverage; needs explicit readiness rule |

## Nested direct reference paths to resolve

`AmbassadorConversationFlows[].Nodes[].StateKey` references `ConversationFlowState` by key, and each node's `Transitions[].TransitionKey` references `ConversationFlowTransitionDefinition` by key. A populated `Transitions[].Handoff.VirtualTeamMemberKey` references another VTM. These are **direct references nested inside a VTM-owned inline object**, not dependencies of an invented parent flow entity. Their exact typed resolver and cycle/ordering semantics require review before the rule can publish.

## Additional direct fields not yet accounted for by the current inspector

- `VideoAvatar`, `ProfileIntroVideo`, and `LandscapeIntroVideo`: check the domain meaning of the EntityHeader IDs and whether each is a `MediaResource` or another domain record.
- `DefaultAgentRole`, `DefaultAgentMode`, `SopExecutionContextRole`, `SopExecutionAgentMode`: the UI addresses roles/modes relative to `DefaultAgentContext`; likely child selectors requiring target-local rebinding, **not standalone EntityBase nodes**. Confirm against the AgentContext model and resolver before publishing.
- Inherited category/product/ownership metadata: inspect whether any header is an external referenced record rather than an inline child/value object. Do not automatically convert every EntityHeader or list into a graph dependency.

## Publication prerequisites

1. Approve the one-level references, presence rules, and environment-binding policies; distinguish actual required data dependencies from readiness-review guidance.
2. Register each exact source property path, target stable type identity, cardinality, permitted compatibility policy, and any known predicate or binding handler in a domain-owned `D3MTypeRegistry`. No reflection-based arbitrary paths.
3. Ensure each referenced target type is registered; then publish **its own** D3M or explicit leaf separately. A referenced type lacking a published D3M blocks graph assembly.
4. Confirm immutable, retrievable **stable source record revisions** before assembling manifests; an approved D3M revision alone does not establish approval of any VTM or dependency payload.
5. Surface draft/validate/publish/read/revisions via an authenticated MCP boundary that derives the authoritative organization and actor from trusted context.

No promotion or bootstrap copy is authorized by this candidate. Formal promotion remains **LIVE target/initiator → DEV source/server**.
