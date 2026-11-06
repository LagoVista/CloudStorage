# CloudStorage Agent Instructions

## Runtime operational controls

The cross-repository authority for live diagnostics switches and TTL-based overrides is:

**[nuviot/platform/docs/runtime-controls.md](https://github.com/nuviot/platform/blob/main/docs/runtime-controls.md)**

Read it before adding or altering operational toggles, logging gates, or Valkey admin workflows.

- Existing implementation: `src/LagoVista.CloudStorage/RuntimeControls/RuntimeControlService.cs` (`IRuntimeControlService`, `RuntimeControlCatalog`).
- Active values are Valkey overrides with optional TTL; missing/expired values use the catalog default.
- New consumers should use the registered control and `IsEnabled` / `GetInt32` rather than ad-hoc Valkey keys or direct hot-path network reads.
- Keep error, warning, and exception diagnostics unconditional. Temporary tracing should be off by default and time bounded when possible.
- Update the System Admin UI's currently hard-coded control list until a server-owned discovery API actually exists.
- Verify the switch's behavior and expiration in DEV, then publish the package and verify that consuming host images contain the intended dependency version. Do not assume deployment alone proves uptake.
- Do not invent a second control service, durable audit trail, or feature-flag contract; the first version has explicit limitations described in the platform guide.

Follow existing repository patterns, and validate changes through exact-commit Build Server proof.
