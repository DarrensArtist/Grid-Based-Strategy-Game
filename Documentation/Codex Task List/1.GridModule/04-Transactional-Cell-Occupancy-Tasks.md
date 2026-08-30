# Grid Module — Slice 4 Codex Task List

> **Slice:** Transactional Cell Occupancy  
+> **Recommended order:** 4 of 10  
+> **Status:** Complete
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Track the authoritative one-entity-per-cell spatial relationship and support atomic placement, movement, removal, and lookup.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

Complete all required earlier slices stated in the source specification; normally begin after Slice 3 is verified.

## Ordered Implementation Tasks

- [x] **Task 1: Define opaque occupant identity and results** — Use stable runtime identifiers without importing unit statistics or action rules; define explicit success/failure reasons.
- [x] **Task 2: Add forward and reverse occupancy indexes** — Track cell-to-occupant and occupant-to-cell within Grid-owned runtime state.
- [x] **Task 3: Implement placement validation and commit** — Validate ready state, identity, active destination, emptiness, and duplicate registration before changing both indexes.
- [x] **Task 4: Implement atomic movement** — Validate source registration and destination completely, then update both directions together. Document same-cell behaviour.
- [x] **Task 5: Implement guarded removal** — Clear only when the requested identity matches the current occupant and keep both indexes consistent.
- [x] **Task 6: Publish post-commit notifications** — Emit one result only after state is consistent; emit nothing misleading for rejected or no-op operations.
- [x] **Task 7: Integrate reset/reload cleanup** — Clear occupancy deliberately when runtime Grid state is replaced.
- [x] **Task 8: Add development consistency scanning** — Detect forward/reverse mismatches and expose useful diagnostics without trying to repair external entity ownership.
- [x] **Task 9: Build sequence and failure tests** — Cover place/move/remove, duplicates, occupied targets, wrong removal identity, inactive/outside cells, stale requests, and long mixed sequences.
- [x] **Task 10: Verify module boundaries** — Confirm no health, movement cost, animation, turn, team-permission, or defeat logic entered Grid.

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

# Slice 4: Transactional Cell Occupancy

## Goal

Track the authoritative one-entity-per-cell spatial relationship and support atomic placement, movement, removal, and lookup.

## Player or Developer Value

This system-facing slice lets later Unit and Action systems use dependable spatial presence while preventing duplicate occupants, inactive placements, and half-completed moves.

## In-Scope Behaviour

- Place one runtime entity on one active unoccupied cell.
- Move an existing occupant between cells atomically.
- Remove an occupant and free its cell.
- Query the occupant of a cell and the cell of an occupant.
- Reject outside, inactive, occupied, duplicated, or stale requests.
- Clear occupancy during deliberate Grid reset/reload.
- Maintain both directions of the relationship consistently.

## Out of Scope

- Unit statistics, Transform animation, movement cost, range, action legality, damage, defeat rules, and turn permission.
- Multi-cell units, stacking, pushing, swapping, or simultaneous path movement.
- Deciding when a defeated unit should be removed; Unit/Action logic requests removal.

## Game Behaviour

A caller requests a spatial mutation with an entity's stable runtime identifier. Grid validates all conditions before changing anything. Successful placement or movement updates cell-to-occupant and occupant-to-cell records together and reports the result. Failure leaves both source and destination unchanged.

## Logical Rules

1. Only active in-bounds cells can be occupied.
2. A cell has zero or one occupant.
3. An occupant is registered on zero or one cell in this Grid.
4. Placement requires an unregistered occupant and empty destination.
5. Movement requires the occupant to be registered at the stated/current source and the destination to be active and empty.
6. All validation precedes mutation.
7. Moving to the current cell is either a documented no-op success or rejection; it must not emit a false movement change.
8. Removal must verify occupant identity before clearing.
9. Grid does not inspect health, team, movement points, or permission.
10. Every successful mutation emits one result after state is consistent.

## State and Data

- **Cell occupant reference/identifier:** mutable match state owned only by Grid.
- **Occupant location index:** mutable reverse lookup owned only by Grid.
- **Occupant identity:** supplied by the owning runtime Module; Grid treats it as opaque spatial identity.
- Occupancy must be included in Grid snapshots but not Arena Grid Profiles.

## Inputs

- Place, move, and remove commands from Unit, Action resolution, Match Setup, or test harness.
- Entity identifier from the entity-owning Module.
- Destination/source coordinates.

## Outputs

- Explicit success or failure with reason to the requester.
- Read-only occupancy/location queries.
- Post-commit occupancy-changed notification for visuals, selection, and diagnostics.

## System Flow

1. Receive a mutation request.
2. Confirm Grid is ready and entity identity is valid.
3. Resolve relevant cells and current registration.
4. Validate every precondition without mutation.
5. Update forward and reverse records as one transaction.
6. Verify consistency in development builds.
7. Publish one completed result and notification.

## Dependencies

### Requires

- Slice 2; Slice 3 is recommended for visual verification.
- Temporary entities with stable identifiers.

### Enables

- Deployment, spatial blockers, action resolution, defeat removal, and snapshots.

## Integration Boundaries

Grid confirms spatial possibility only. The Action Module decides whether a unit may move; Unit owns the unit and records its coordinate in agreement with successful Grid results. A temporary entity registry substitutes until Unit exists.

## Editor and Authoring Support

No persistent occupancy authoring is allowed. A play-mode test panel may spawn identifiers and request placement/movement/removal.

## Debug and Observability

- Occupant identifiers over occupied cells.
- Selected occupant's reverse lookup.
- Mutation log with request, rejection reason, and final source/destination.
- Development-only full consistency scan.

## Edge Cases and Failure Handling

- Destination occupied: reject with occupant-independent reason; preserve source.
- Duplicate placement: reject and report existing coordinate.
- Remove wrong occupant: reject without clearing.
- Move after Grid reload: reject stale request.
- Entity disappears externally: diagnostic consistency scan identifies unresolved identity; recovery policy belongs to integration.

## Acceptance Criteria

- [x] Valid placement updates both lookup directions.
- [x] Invalid placement changes neither lookup.
- [x] Valid movement frees the source and occupies the destination atomically.
- [x] Failed movement leaves the occupant at its original cell.
- [x] Removal frees the cell and reverse index.
- [x] Inactive/outside coordinates never accept occupants.
- [x] Consistency scan finds no mismatch after every supported operation sequence.

## Suggested Verification

- **Normal:** place, move, query, and remove one test entity.
- **Invalid:** move into an occupied cell and compare state before/after.
- **Boundary:** use active cells adjacent to inactive/outside coordinates.
- **Integration:** update a placeholder entity's visual only after a success notification.

## Completion State

The Grid reliably owns spatial occupancy without owning the entities or their gameplay permissions.

---
