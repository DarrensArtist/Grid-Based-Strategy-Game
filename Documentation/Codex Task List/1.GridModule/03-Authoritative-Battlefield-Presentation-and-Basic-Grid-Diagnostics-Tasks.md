# Grid Module — Slice 3 Codex Task List

> **Slice:** Authoritative Battlefield Presentation and Basic Grid Diagnostics  
+> **Recommended order:** 3 of 10  
+> **Status:** Complete  
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Display a loaded logical battlefield in Unity using the Grid as the sole source of layout and position truth.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

Complete all required earlier slices stated in the source specification; normally begin after Slice 2 is verified.

## Ordered Implementation Tasks

- [x] **Task 1: Confirm runtime presentation boundary** — Read the ready Grid through public queries only; do not make scene objects authoritative.
- [x] **Task 2: Create the battlefield presenter** — Subscribe to ready/reset lifecycle notifications and own only transient presentation instances.
- [x] **Task 3: Render active cells** — Create one placeholder surface per active cell at the authoritative returned centre; create nothing interactive for inactive coordinates.
- [x] **Task 4: Add zone styling and safe fallbacks** — Apply temporary Team A/Neutral/Team B styling and degrade cleanly when optional materials are missing.
- [x] **Task 5: Implement rebuild and disposal** — Clear only presenter-owned instances and deterministically rebuild from the same runtime state.
- [x] **Task 6: Add independent diagnostic layers** — Support root, footprint, coordinates, centres, stable identities, and zone toggles, disabled by default at runtime.
- [x] **Task 7: Support transformed roots** — Ensure surfaces and overlays remain aligned after Grid-root translation or rotation.
- [x] **Task 8: Add presentation tests** — Verify active-only rendering, zone mapping, unchanged logical state after visual drift, safe pre-ready behaviour, and deterministic rebuild.
- [x] **Task 9: Profile a representative large grid** — Confirm labels can be disabled and placeholder generation is acceptable for development use.
- [x] **Task 10: Run verification and report** — Check compilation/tests and list any temporary assets or future replacement seams.

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

- `BattlefieldPresenter` binds to `RuntimeGrid`, subscribes to initialisation/reload results, and owns only a transient child hierarchy. Rebuild and disposal never modify logical cells.
- Active surfaces use an XZ quad without colliders. Optional meshes and materials are replaceable; missing zone materials receive generated blue/grey/red unlit fallbacks.
- Presentation children are parented beneath the authoritative Grid root, so later translation and rotation remain aligned. Every initial local position is derived from `TryGetCellCentre`.
- Runtime diagnostic toggles for root, boundary, footprint, centres, zones, coordinates, and stable identities default off. Coordinate/identity labels and the compact metadata panel are editor-only.
- `DevelopmentArenaGridProfile` and the generated fallback mesh/materials are temporary development assets/seams, not production presentation.
- A 32×32 all-active fixture generated 1,024 placeholder surfaces with labels disabled inside the test's 10-second development ceiling.
- Verification: Unity `6000.4.12f1` imported the profile, loaded the actual `GridModule` scene, and passed all 49 Edit Mode tests with 0 failures, skips, or inconclusive results and no C# compiler warnings or errors.

---

## Authoritative Slice Specification

# Slice 3: Authoritative Battlefield Presentation and Basic Grid Diagnostics

## Goal

Display a loaded logical battlefield in Unity using the Grid as the sole source of layout and position truth.

## Player or Developer Value

This is the first visible slice. Developers and players can see which spaces exist, the three arena regions, and where cell centres lie, while later art can replace placeholders safely.

## In-Scope Behaviour

- Create a simple visual representation for active cells.
- Do not create interactive surfaces for inactive coordinates.
- Visually distinguish Team A, neutral, and Team B regions using temporary materials or overlays.
- Place every visual through the Grid's cell-centre conversion.
- Rebuild presentation from runtime data without changing logical state.
- Expose independent diagnostic toggles for coordinates, centres, identities, zones, root, and boundary.

## Out of Scope

- Production arena art, walls, banners, lighting, shaders, and animation.
- Normal gameplay highlighting and selection.
- Unit visuals.
- Runtime path/query overlays beyond the basic cell facts; expanded in Slices 5 and 8.

## Game Behaviour

After the Grid becomes ready, a presentation consumer reads active cells and creates unobtrusive square surfaces centred on them. Gaps remain where coordinates are inactive. Decorative geometry may surround the result but never changes cell facts. Rebuilding or hiding the visuals leaves the logical Grid untouched.

## Logical Rules

1. Presentation begins only after a ready Grid is published.
2. Each active cell has at most one base cell visual in this presenter.
3. Every visual position comes from Grid conversion.
4. Visual Transform drift never changes a coordinate or cell state.
5. Diagnostic visibility changes presentation only.
6. Destroying/rebuilding presentation preserves the same logical layout.
7. Editor-only display code is excluded from release builds; runtime diagnostics are off by default.

## State and Data

- **Presentation instances/material references:** transient visual state owned by the presenter, not Runtime Grid State.
- **Diagnostic toggle state:** transient development preference, never saved as match state.
- **Logical cell facts:** read-only inputs owned by Grid runtime.

## Inputs

- Grid-ready and Grid-reset notifications from the Grid lifecycle.
- Read-only active cell collection, zone, identity, and centre queries.
- Developer diagnostic toggle commands.

## Outputs

- Visible battlefield surfaces and optional labels/gizmos for humans.
- Presentation-ready/failure diagnostic to the runtime host.

## System Flow

1. Observe a ready Grid.
2. Clear presentation belonging to the previous Grid instance.
3. Enumerate active cells.
4. Request each centre and create its zone-styled placeholder.
5. Draw the logical boundary and any enabled diagnostic layers.
6. On rebuild/reset, dispose only visual instances and regenerate from authoritative data.

## Dependencies

### Requires

- Slices 1 and 2.

### Enables

- Visual verification of occupancy, queries, deployment, validation, and editor previews.

## Integration Boundaries

The presenter reads Grid facts. Highlighting later supplies separate transient overlays and must not modify cell appearance data inside Grid cells.

## Editor and Authoring Support

Expose placeholder meshes/materials, label size, diagnostic colours, and master visibility controls. Defaults must work without manual per-cell setup.

## Debug and Observability

- Independent toggles for coordinate, stable identity, centre, zone, active footprint, and arena boundary.
- Display source profile metadata and Grid status in one compact panel.
- A rebuild command proves visual state is derived.

## Edge Cases and Failure Handling

- No ready Grid: show no battlefield and report why.
- Missing placeholder material: use a safe fallback and warning.
- Large grid: allow labels to be disabled independently.
- Rotated root: visuals and overlays follow the root correctly.

## Acceptance Criteria

- [x] Only active cells receive visible playable surfaces.
- [x] Region colours match authoritative zone data.
- [x] All surfaces are centred through Grid conversion.
- [x] Moving a visual manually does not alter Grid state.
- [x] Rebuilding produces the same footprint and cell identities.
- [x] Diagnostic layers can be enabled separately and are disabled by default at runtime.

## Suggested Verification

- **Normal:** display a cut-corner profile with three regions.
- **Invalid:** attempt presentation before initialisation and observe a useful message rather than errors.
- **Boundary:** display odd and even profiles and inspect their centres.
- **Integration:** move/rotate the Grid root, rebuild, and confirm alignment.

## Completion State

The logical Grid can be seen and inspected in Unity without surrendering authority to scene objects.

---
