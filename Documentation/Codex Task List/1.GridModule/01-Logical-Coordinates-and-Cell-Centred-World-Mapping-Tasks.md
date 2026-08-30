# Grid Module — Slice 1 Codex Task List

> **Slice:** Logical Coordinates and Cell-Centred World Mapping  
+> **Recommended order:** 1 of 10  
+> **Status:** Complete  
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Establish the Grid's coordinate convention and provide reliable conversion between logical coordinates and cell-centre positions relative to a centred Grid root.

Once complete, odd- and even-sized rectangular grids can be reasoned about and positioned consistently without creating runtime cells or loading an Arena Grid Profile.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

None beyond Unity transform/test foundations.

## Ordered Implementation Tasks

- [x] **Task 1: Confirm project conventions** — Inspect the Unity version, assembly definitions, namespace rules, existing test layout, and repository instructions. Record any assumptions in the implementation notes before editing.
- [x] **Task 2: Create the coordinate value type** — Implement an immutable GridCoordinate with x/z integer components, value equality, hashing, readable formatting, and no Unity Transform authority.
- [x] **Task 3: Create validated geometry configuration** — Represent width, height, and cell size with explicit validation and useful failure reasons. Keep this independent of Arena Grid Profile loading.
- [x] **Task 4: Implement backing-grid containment** — Add the single authoritative bounds check for 0 <= x < width and 0 <= z < height.
- [x] **Task 5: Implement grid-to-world conversion** — Use the centred-cell formula from the specification, then transform through the Grid root. Return failure for invalid coordinates.
- [x] **Task 6: Implement world-to-grid conversion** — Inverse-transform the point, ignore local Y, enforce outer half-cell limits, and apply one documented deterministic internal-boundary tie rule.
- [x] **Task 7: Expose a narrow mapping API** — Ensure consuming modules call the mapper rather than duplicating offsets or formulas. Avoid profile, cell, occupancy, or selection responsibilities.
- [x] **Task 8: Add focused edit-mode tests** — Cover odd/even dimensions, all-coordinate round trips, invalid configuration, outside points, exact edges, boundary ties, translated roots, and rotated roots.
- [x] **Task 9: Add minimal diagnostics** — Provide optional centre/root/footprint inspection suitable for development without creating production grid visuals.
- [x] **Task 10: Verify the slice in isolation** — Run the relevant tests, check compilation, and document changed files, the chosen tie rule, and any remaining assumptions.

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

- Implemented against Unity `6000.4.12f1` with namespace `GridBasedStrategyGame.Grid` and separate runtime/Edit Mode test assemblies.
- Exact internal cell boundaries resolve toward the positive X/Z axis. Exact outer footprint edges are inclusive and resolve to their edge cells.
- `GridMappingDiagnostics` is an optional, disabled-by-absence development component. Its serialized geometry is temporary Slice 1 configuration and is not a content format.
- Non-uniform Grid-root scale is not an intended authoring configuration for the initial Grid Module, consistent with the task-pack assumption.
- Verification: Unity Edit Mode test run on 2026-08-30 passed all 18 tests with 0 failures, skips, or inconclusive results. Unity exited with code 0 and emitted no C# compiler warnings or errors in the final run.

---

## Authoritative Slice Specification

# Slice 1: Logical Coordinates and Cell-Centred World Mapping

## Goal

Establish the Grid's coordinate convention and provide reliable conversion between logical coordinates and cell-centre positions relative to a centred Grid root.

Once complete, odd- and even-sized rectangular grids can be reasoned about and positioned consistently without creating runtime cells or loading an Arena Grid Profile.

## Player or Developer Value

This is foundational. Every unit, marker, effect, pointer conversion, query, and saved position will share one spatial convention, preventing half-cell offsets and Transform positions from becoming gameplay authority.

## In-Scope Behaviour

- Represent two-dimensional integer coordinates from `(0, 0)` through `(width - 1, height - 1)`.
- Validate whether a coordinate lies inside the rectangular backing grid.
- Calculate a coordinate's cell centre from width, height, cell size, and the Grid root.
- Convert a world position back to the corresponding backing-grid coordinate.
- Support translated and rotated Grid roots while keeping logical coordinates unchanged.
- Ignore Unity Y when resolving the two-dimensional coordinate, while preserving the Grid root's floor plane for returned world positions.
- Support odd and even dimensions without special-case offsets in consuming Modules.

## Out of Scope

- Active/inactive cells and Arena Grid Profiles; Slice 2.
- Visual cell generation; Slice 3.
- Occupancy, spatial queries, and gameplay range rules; later slices.
- Terrain height, stacked cells, or multi-level arenas; future extension.

## Game Behaviour

Other systems submit a logical coordinate and receive the centre of its square cell in world space. A coordinate is first checked against the backing-grid dimensions. Valid coordinates are positioned relative to the centred Grid root; invalid coordinates return a failed result rather than a fabricated position.

For world-to-grid conversion, the world point is transformed into Grid-root local space, measured against the cell-centre layout, and resolved to one backing-grid coordinate. A point outside the rectangular footprint returns failure. This operation reports geometry only; it does not yet determine whether the resolved coordinate is active.

## Logical Rules

1. Width and height must each be positive integers.
2. Cell size must be greater than zero.
3. A coordinate is inside the backing grid only when `0 <= x < width` and `0 <= z < height`.
4. Local cell-centre X is `(x - (width - 1) / 2) × cell size`.
5. Local cell-centre Z is `(z - (height - 1) / 2) × cell size`.
6. The local centre is transformed through the Grid root to obtain world space.
7. World-to-grid applies the inverse Grid-root transform before resolving a coordinate.
8. Points exactly on an internal cell boundary use one documented deterministic tie rule; no caller may supply its own half-cell correction.
9. Points beyond the outer half-cell edges fail conversion.
10. Converting a valid coordinate to its centre and back must return the original coordinate.

## State and Data

- **Grid Coordinate:** immutable logical value owned by the Grid Module. It may be created by any caller but has no runtime lifecycle and is saved only as part of another owned state object.
- **Grid geometry parameters:** width, height, and cell size. Configuration inputs for this slice's test harness; later sourced from the Arena Grid Profile.
- **Grid root transform:** runtime presentation context owned by the Grid instance. It may move or rotate without rewriting logical coordinates.

## Inputs

- Width, height, and cell size from temporary test configuration; later from the loaded profile.
- Grid root transform from the Unity scene/runtime Grid host.
- Logical coordinate from any spatial consumer.
- World position from pointer, effect, or diagnostic consumers.

## Outputs

- Backing-grid containment result for callers.
- World-space cell centre for visual consumers.
- Successful coordinate result or explicit failure for world-position consumers.

## System Flow

1. Receive geometry parameters and reject invalid values.
2. For grid-to-world, validate the coordinate.
3. Calculate its local cell centre and transform it through the Grid root.
4. For world-to-grid, inverse-transform the point to local space.
5. Test the point against the rectangular footprint.
6. Resolve it using the documented boundary tie rule, validate the result, and return success or failure.

## Dependencies

### Requires

- Unity's normal transform space or an equivalent testable transform abstraction.

### Enables

- Runtime Grid construction, visuals, pointer resolution, placement, queries, and restoration.

## Integration Boundaries

Consumers request conversion; they do not reproduce the formula. Until Selection exists, a test ray or manually supplied world point is sufficient. This slice does not perform camera raycasts.

## Editor and Authoring Support

A small development inspector may expose width, height, cell size, and Grid-root transform for testing. It is not the Arena Layout Editor and should not become a content format.

## Debug and Observability

- Optional labels for coordinates and their calculated centres.
- A marker for the Grid root and the rectangular footprint.
- A test command that displays the coordinate resolved from a chosen world point.
- Diagnostic output showing local-space input when conversion fails.

## Edge Cases and Failure Handling

- Zero/negative dimensions or cell size: reject initialisation with a clear diagnostic.
- Even dimensions: root lies between the central four cells.
- Odd dimensions: the central cell lies on the root.
- Boundary point: use the single documented tie rule.
- Translated/rotated root: conversion remains reversible.
- World point far above/below the floor: Y is ignored for coordinate resolution; height validation belongs to the caller if needed.

## Acceptance Criteria

- [x] A 9×9 grid maps `(4, 4)` to the Grid root.
- [x] A 10×10 grid maps its central four cells to `±0.5 × cell size` on both local axes.
- [x] Every valid test coordinate round-trips through its centre.
- [x] Invalid coordinates and outside world points return failure without a usable fabricated result.
- [x] Moving or rotating the Grid root changes world results but not logical coordinates.
- [x] No public operation requires a caller-applied half-cell offset.

## Suggested Verification

- **Normal:** round-trip all coordinates in a 9×9 grid.
- **Invalid:** try zero cell size and coordinate `(-1, 0)`; both are rejected with distinct reasons.
- **Boundary:** test all cells of a 10×10 grid and points on outer/internal edges.
- **Integration:** place a marker at a returned centre under a rotated root and convert its Transform position back.

## Completion State

The project has one authoritative, reversible convention for logical coordinates and cell-centred world placement.

---
