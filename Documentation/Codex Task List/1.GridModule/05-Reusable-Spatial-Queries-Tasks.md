# Grid Module — Slice 5 Codex Task List

> **Slice:** Reusable Spatial Queries  
+> **Recommended order:** 5 of 10  
+> **Status:** Not started  
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Expose deterministic cell collections and directional geometry for adjacency, lines, rectangles, and areas around a coordinate.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

Complete all required earlier slices stated in the source specification; normally begin after Slice 4 is verified.

## Ordered Implementation Tasks

- [ ] **Task 1: Define direction and query contracts** — Create the eight planar directions, deterministic ordering, explicit origin-inclusion rules, distance modes, and occupancy filter modes.
- [ ] **Task 2: Implement neighbour queries** — Return cardinal, diagonal, or combined active neighbours with stable ordering.
- [ ] **Task 3: Implement directional line queries** — Walk coordinate geometry and terminate at the first inactive/outside coordinate without re-entering beyond gaps.
- [ ] **Task 4: Implement rectangle queries** — Apply the documented normalise-or-reject policy, clip to active cells, de-duplicate, and preserve ordering.
- [ ] **Task 5: Implement area/group queries** — Support explicitly named Manhattan, Chebyshev, or approved distance modes so semantics cannot be confused.
- [ ] **Task 6: Add edge reporting** — Expose playable arena edges as geometry facts without inferring movement or targeting legality.
- [ ] **Task 7: Apply optional occupancy filters** — Use Slice 4 state only when requested; default geometry must not stop at occupied cells.
- [ ] **Task 8: Expose immutable results** — Return read-only values and explicit invalid-request failures without mutating Grid state.
- [ ] **Task 9: Add query diagnostics** — Allow the last query, order indices, direction, bounds, and termination reason to be visualised independently.
- [ ] **Task 10: Create topology tests** — Cover centre/edge/corner, concave gaps, cut corners, zero sizes, invalid origins, repeatable ordering, filters, and de-duplication.

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

---

## Authoritative Slice Specification

# Slice 5: Reusable Spatial Queries

## Goal

Expose deterministic cell collections and directional geometry for adjacency, lines, rectangles, and areas around a coordinate.

## Player or Developer Value

This system-facing slice gives Action and Highlighting consistent geometry. Movement and attacks can later interpret shared query results instead of rebuilding spatial maths independently.

## In-Scope Behaviour

- Return cardinal and diagonal neighbours separately or together.
- Represent the eight planar directions.
- Trace a line from an origin in a direction until a requested length or arena termination.
- Return cells in an axis-aligned rectangular coordinate region.
- Return groups around a position using an explicitly selected supported distance/shape mode.
- Report playable arena edges.
- Optionally filter occupied/unoccupied results only when explicitly requested; occupancy does not inherently stop geometry.
- Stop directional traversal at inactive coordinates and never re-enter beyond a gap.
- Return results in documented deterministic order.

## Out of Scope

- Pathfinding, reachability, movement cost, line of sight, cover, weapon range, targeting legality, or damage patterns.
- Visual highlighting of results.
- Terrain or elevation.

## Game Behaviour

A consumer supplies an origin and query definition. Grid validates the request, walks logical geometry, excludes non-playable coordinates according to the query contract, and returns an ordered read-only result. It does not label results legal moves or targets.

## Logical Rules

1. Query origins must be active unless a diagnostic API explicitly allows backing-grid inspection.
2. Cardinal neighbours differ by one on exactly one axis.
3. Diagonal neighbours differ by one on both axes.
4. Outside and inactive candidates are excluded from neighbour/region results.
5. A directional line terminates on the first outside or inactive coordinate.
6. A line does not resume after an inactive gap.
7. Occupancy affects results only through an explicit filter; it does not redefine topology.
8. Rectangle bounds are normalised or rejected consistently and clipped to playable active cells.
9. Radius/group semantics are named explicitly so Manhattan, Chebyshev, and Euclidean-like interpretations cannot be confused.
10. Query results contain Grid facts, not Action rulings.

## State and Data

- **Direction definitions and ordering:** immutable Grid rules.
- **Query request parameters/results:** transient values, not saved.
- **Layout and occupancy:** read-only runtime inputs owned by Grid.

## Inputs

- Origin, direction, length, bounds, radius/shape mode, and optional occupancy filter from Action, Highlighting, AI, or diagnostics.

## Outputs

- Ordered read-only coordinates/cells and explicit invalid-request results.
- Optional query-description data for diagnostics.

## System Flow

1. Validate Grid state, origin, and parameters.
2. Select the requested geometry rule.
3. Generate candidates in documented order.
4. Enforce backing-grid and active-cell rules.
5. For lines, terminate at the first logical gap.
6. Apply explicit filters.
7. Return results without mutating Grid state.

## Dependencies

### Requires

- Slice 2; Slice 4 for occupancy filters.

### Enables

- Action patterns, AI inspection, selection details, highlighting, and connectivity validation.

## Integration Boundaries

Action decides how a line or region affects units. Highlighting only displays supplied collections. Grid provides no `GetLegalMoves` or `GetAttackTargets` operation.

## Editor and Authoring Support

No content asset is required. A query test panel should allow origin, query type, direction, length/radius, and filter selection.

## Debug and Observability

- Overlay the last query with ordered indices.
- Draw direction rays and region bounds.
- Show termination coordinate and reason.
- Keep query overlays independently toggleable.

## Edge Cases and Failure Handling

- Inactive/outside origin: fail explicitly.
- Zero length/radius: follow the documented origin-inclusion rule.
- Line hits occupied cell: include/skip/stop only as the caller's explicit query mode defines; default geometry continues.
- Concave arena: rectangles omit inactive cells; lines stop at gaps.
- Duplicate candidates: de-duplicate while preserving deterministic order.

## Acceptance Criteria

- [ ] Centre, edge, and corner neighbour queries return correct cardinal/diagonal sets.
- [ ] Lines stop at the first inactive coordinate and never re-enter.
- [ ] Rectangle and group results contain only active cells.
- [ ] Repeating a query returns the same ordering.
- [ ] Occupancy filtering never mutates occupancy.
- [ ] Query names/results do not claim movement or attack legality.

## Suggested Verification

- **Normal:** run each query type from a central cell of a rectangle.
- **Invalid:** query from inactive and outside coordinates.
- **Boundary:** trace into a cut corner and across a concave gap.
- **Integration:** pass returned cells to a temporary overlay without allowing the overlay to query hidden Grid state.

## Completion State

Other systems can consume one consistent vocabulary of arena geometry without transferring gameplay-rule ownership into the Grid.

---
