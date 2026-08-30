# Grid Module — Slice 2 Codex Task List

> **Slice:** Arena Profile Loading and Logical Battlefield Construction  
+> **Recommended order:** 2 of 10  
+> **Status:** Complete  
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Load one reusable Arena Grid Profile and construct an immutable-layout, mutable-runtime logical battlefield containing active state and deployment-zone assignments.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

Complete all required earlier slices stated in the source specification; normally begin after Slice 1 is verified.

## Ordered Implementation Tasks

- [x] **Task 1: Inspect Slice 1 APIs** — Confirm the implementation uses the existing coordinate and mapping authority without copying formulas.
- [x] **Task 2: Define ArenaGridProfile** — Create the reusable ScriptableObject configuration with identity, schema version, dimensions, cell size, active layout, zone layout, checksum/summary fields, and designer notes.
- [x] **Task 3: Define runtime cell and lifecycle types** — Add cell identity, active state, zone, source metadata, and Uninitialised/Initialising/Ready/Failed lifecycle state with read-only public access.
- [x] **Task 4: Implement structural profile validation** — Reject null, unsupported, malformed, or inconsistent payloads before a ready grid can be published.
- [x] **Task 5: Build candidate runtime state** — Allocate deterministic backing entries, apply active/zone facts, and derive stable cell identities without mutating the profile.
- [x] **Task 6: Commit initialisation atomically** — Publish the candidate only after all checks pass; otherwise discard it and expose a structured failure.
- [x] **Task 7: Implement read-only grid queries** — Expose geometry, source metadata, cell existence, playable state, zone, identity, and centres without leaking mutable collections.
- [x] **Task 8: Handle deliberate reloads** — Require an explicit lifecycle operation and replace rather than merge runtime layout state.
- [x] **Task 9: Create representative profile fixtures** — Add valid odd/even profiles plus malformed payload, invalid zone, empty layout, and unsupported-version cases.
- [x] **Task 10: Test and document** — Verify atomic failure, source immutability, counts, zones, stable identities, and Slice 1 centre integration.

## Required Handoff Evidence

When the slice is complete, Codex must provide:

- A concise summary of implemented behaviour.
- A list of files created or modified.
- Tests added and the commands/results used to run them.
- Any design decisions that were not already fixed by this document.
- Any remaining risks, assumptions, or integration work explicitly deferred to later slices.
- A direct checklist comparison against every acceptance criterion below.

## Definition of Done

The slice is done only when:

- Every ordered task is completed or explicitly marked blocked with evidence.
- All acceptance criteria in the source specification pass.
- Relevant edit-mode/play-mode tests pass and the Unity project compiles without new warnings caused by this slice.
- Public APIs preserve the module boundaries and do not duplicate an earlier slice's authority.
- Diagnostics fail clearly and do not fabricate usable state after invalid input.
- Documentation/comments explain non-obvious rules, especially deterministic ordering, boundary behaviour, and atomicity.

## Implementation Notes

- `ArenaGridProfile` stores row-major cell definitions (`index = z * width + x`) and remains reusable configuration; runtime construction copies its facts into an independently owned candidate state.
- Runtime loading supports schema version `1`. Profile identity and layout checksum are required and cached as source metadata.
- Stable cell identities use the deterministic format `{profileId}:{x}:{z}` and each runtime cell also exposes its source profile identity explicitly.
- Empty active layouts are rejected by the runtime loader. A later authoring tool may support an explicit non-runtime preview path without weakening runtime safety.
- `Initialize` rejects accidental replacement of a ready Grid; `Reload` is the deliberate replacement operation. A failed reload publishes neither stale state nor a partial candidate.
- Verification: Unity `6000.4.12f1` Edit Mode run on 2026-08-30 passed all 40 Grid tests with 0 failures, skips, or inconclusive results and no C# compiler warnings or errors.

---

## Authoritative Slice Specification

# Slice 2: Arena Profile Loading and Logical Battlefield Construction

## Goal

Load one reusable Arena Grid Profile and construct an immutable-layout, mutable-runtime logical battlefield containing active state and deployment-zone assignments.

## Player or Developer Value

This foundational and content-facing slice turns authored arena data into a dependable runtime Grid that later systems can query without understanding profile storage.

## In-Scope Behaviour

- Define the configuration/runtime separation between Arena Grid Profile and Runtime Grid State.
- Load profile identity, schema version, dimensions, cell size, active-cell layout, and zone layout.
- Allocate one runtime cell for every backing-grid coordinate.
- Mark inactive coordinates as non-playable rather than blocked terrain.
- Assign each active cell to Team A Deployment, Neutral No Man's Land, or Team B Deployment.
- Give every allocated cell a stable identity derived deterministically from profile and coordinate.
- Track Grid lifecycle status: uninitialised, initialising, ready, or failed.
- Reject profiles with structurally unsafe data before publishing a ready Grid.

## Out of Scope

- Full gameplay-readiness validation; Slice 8.
- Visual display; Slice 3.
- Occupancy; Slice 4.
- Custom editor; Slices 9 and 10.
- Matchmaking selection of which profile to load; Match Runtime.

## Game Behaviour

At match setup, Match Runtime supplies the selected profile. The Grid validates essential structure, creates its cells, applies active and zone facts, and becomes ready atomically. Other Modules cannot query a partially constructed Grid. If construction fails, the previous Grid is not silently combined with the new one, and failure information identifies the profile and cause.

Inactive coordinates remain represented internally only where useful for rectangular indexing and diagnostics. Public playable-cell queries treat them as nonexistent.

## Logical Rules

1. Only one profile defines one runtime Grid instance.
2. The profile remains reusable configuration and is never modified by match state.
3. Every coordinate inside the backing rectangle maps to one allocated cell record or an equivalent deterministic lookup entry.
4. Only active cells may have a gameplay zone.
5. Active cells must have exactly one zone.
6. Neutral cells cannot be deployment cells.
7. Runtime construction is all-or-nothing.
8. A failed Grid cannot report itself ready.
9. A successful Grid exposes the source profile identity, version, and checksum.
10. Reloading deliberately replaces runtime state; it does not merge layouts.

## State and Data

- **Arena Grid Profile:** persistent ScriptableObject owned by Grid content. Designers modify it through approved authoring tools; runtime systems read it only.
- **Runtime Grid State:** match-lifetime state owned and modified only by the Grid Module. Other Modules may query it.
- **Grid Cell:** runtime spatial record containing coordinate, stable identity, active state, zone, and later occupancy.
- **Runtime status/failure details:** transient Grid-owned state used by Match Runtime and diagnostics.

## Inputs

- Selected Arena Grid Profile from Match Runtime or a temporary test loader.
- Grid root transform from the runtime scene.

## Outputs

- Ready/failed initialisation result to Match Runtime.
- Queries for cell existence, active state, zone, identity, dimensions, source profile, and cell centre.
- Initialisation diagnostic event for development tooling.

## System Flow

1. Receive a profile and enter initialising state.
2. Verify reference, schema support, dimensions, cell size, and layout payload consistency.
3. Allocate candidate runtime cells.
4. Apply active and zone configuration.
5. Calculate stable identities and cache profile metadata.
6. Verify runtime counts against profile metadata where present.
7. Publish the complete state and enter ready, or discard the candidate state and enter failed.

## Dependencies

### Requires

- Slice 1 coordinate and mapping rules.
- A hand-authored test profile or fixture.

### Enables

- Visual display, occupancy, queries, deployment, snapshots, validation, and authoring previews.

## Integration Boundaries

Match Runtime chooses and requests loading. The Grid owns construction. Unit and Action consumers receive read-only facts. No external Module receives mutable cell collections.

## Editor and Authoring Support

Provide an initial ScriptableObject asset with clearly grouped identity, geometry, layout, zone, version, checksum, summary, and notes fields. Raw layout fields may be visible read-only for diagnosis; manual array editing is not the intended final workflow.

## Debug and Observability

- Inspect loaded profile identity/version/checksum and runtime status.
- List cell coordinate, identity, active state, and zone.
- Log one structured failure containing the failing validation and profile identity.
- Show active/zone colours in a basic diagnostic view.

## Edge Cases and Failure Handling

- Null or unsupported profile: fail before allocation.
- Layout length/dimensions disagree: fail without publishing cells.
- Active cell lacks or has multiple zones: fail.
- Empty playable layout: structural load may succeed only if explicitly allowed for editor preview; it cannot be Gameplay Ready.
- Reinitialisation while ready: require an explicit lifecycle request, not an accidental second call.

## Acceptance Criteria

- [x] A valid profile creates the expected number of backing entries and active cells.
- [x] Every active cell reports exactly one correct zone.
- [x] Inactive coordinates are rejected by playable-cell queries.
- [x] Runtime mutation does not change the source asset.
- [x] Invalid profile construction publishes no partial ready state.
- [x] Profile identity, version, and checksum are inspectable at runtime.

## Suggested Verification

- **Normal:** load a small symmetrical fixture with all three regions and compare every cell.
- **Invalid:** load a profile whose payload size differs from its dimensions.
- **Boundary:** load minimum supported odd and even dimensions.
- **Integration:** ask the Slice 1 mapper for centres of active corner cells from loaded dimensions.

## Completion State

The project can load reusable arena configuration and expose a complete authoritative logical battlefield.

---
