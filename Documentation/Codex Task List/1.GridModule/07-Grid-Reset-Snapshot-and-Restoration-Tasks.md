# Grid Module — Slice 7 Codex Task List

> **Slice:** Grid Reset, Snapshot, and Restoration  
+> **Recommended order:** 7 of 10  
+> **Status:** Not started  
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Capture, reset, and restore the logical Grid state required by a match without duplicating reusable arena layout data.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

Complete all required earlier slices stated in the source specification; normally begin after Slice 6 is verified.

## Ordered Implementation Tasks

- [ ] **Task 1: Define the Grid snapshot schema** — Store snapshot schema, profile fingerprint, dimensions, and occupant identity-to-coordinate data only; exclude reusable layout and visual state.
- [ ] **Task 2: Implement deterministic capture** — Read authoritative ready Grid occupancy and produce stable serialisable data.
- [ ] **Task 3: Implement reset** — Replace current occupancy with empty occupancy while retaining the loaded profile and layout.
- [ ] **Task 4: Define profile compatibility checks** — Validate profile identity, supported version policy, checksum, and dimensions before accepting occupant entries.
- [ ] **Task 5: Integrate an external occupant resolver** — Resolve saved identities through the owning system without serialising or recreating external module state inside Grid.
- [ ] **Task 6: Build and validate a restoration candidate** — Reject inactive coordinates, duplicates, unresolved identities, schema errors, and incompatible profiles before mutation.
- [ ] **Task 7: Commit restoration atomically** — Replace occupancy only after all checks succeed and publish lifecycle/occupancy notifications afterwards.
- [ ] **Task 8: Add diagnostic inspection controls** — Support in-memory capture, reset, corruption tests, diff inspection, and precise failure reasons.
- [ ] **Task 9: Create round-trip and negative tests** — Cover empty/full capture, valid restore, corrupted coordinates, duplicate identities/cells, missing profiles, unresolved entities, and busy lifecycle.
- [ ] **Task 10: Verify presentation integration** — Rebuild Slice 3 after restoration and confirm odd/even logical coordinates remain centred.

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

# Slice 7: Grid Reset, Snapshot, and Restoration

## Goal

Capture, reset, and restore the logical Grid state required by a match without duplicating reusable arena layout data.

## Player or Developer Value

This system-facing slice supports match restart, save/load integration, recovery, and deterministic testing while protecting against restoring occupancy onto the wrong arena revision.

## In-Scope Behaviour

- Reset occupancy to the loaded profile's empty initial Grid.
- Capture profile identity/version/checksum, dimensions, snapshot schema version, and occupancy.
- Restore a snapshot only when profile compatibility and every occupancy entry validate.
- Resolve saved occupant identifiers through an external resolver supplied by the entity-owning system.
- Apply restoration atomically.
- Report specific initialisation/restoration failures.

## Out of Scope

- Saving unit health, race, abilities, turns, recovery, selections, highlights, or private formations.
- File format, save slots, cloud storage, autosave policy, or full-match orchestration.
- Migrating arbitrary historical profile layouts automatically.

## Game Behaviour

Match Runtime requests a snapshot or reset. A snapshot records only the Grid-owned live state plus a reference fingerprint for the reusable profile. During restoration, Match Runtime loads the referenced compatible profile and makes entity identities resolvable. Grid validates the complete candidate state before replacing current occupancy. Failure leaves the current Grid in a known unchanged or explicitly failed lifecycle state, never partially restored.

## Logical Rules

1. A snapshot never duplicates the full active/zone layout.
2. Snapshot schema version is independent from profile schema version.
3. Profile identity, compatible version policy, checksum, and dimensions are checked before occupancy.
4. Every saved coordinate must resolve to an active cell.
5. Every occupant identity and coordinate must be unique.
6. Every identity must resolve through the owning entity system before commit.
7. Entire restoration validates before mutation.
8. Reset clears all occupancy but retains the loaded profile/layout.
9. Snapshot capture reads only authoritative logical state, never visual Transforms.
10. Restoration emits state-change notifications only after a successful commit.

## State and Data

- **Grid Snapshot:** persistent serialisable data owned by Grid but stored/orchestrated by the Save/Match Runtime system.
- **Snapshot schema version:** persistent compatibility marker owned by Grid.
- **Profile fingerprint:** persistent reference metadata.
- **Occupant identity mapping:** persistent identifiers in snapshot; live object resolution remains owned externally.
- **Restoration candidate:** transient Grid-owned state discarded on failure.

## Inputs

- Capture/reset/restore commands from Match Runtime.
- Snapshot data from the save system.
- Compatible Arena Grid Profile from profile catalogue/runtime.
- Occupant resolver from Unit/runtime entity ownership.

## Outputs

- Grid Snapshot to the save orchestrator.
- Reset/restoration result and precise failure details.
- Post-commit occupancy/lifecycle notifications.

## System Flow

1. For capture, verify ready state and serialise profile fingerprint plus occupancy.
2. For reset, build empty occupancy, replace current occupancy, and notify.
3. For restore, validate snapshot schema and profile fingerprint.
4. Load/confirm the profile and construct candidate Grid state.
5. Resolve and validate every occupant entry.
6. Commit the candidate atomically or discard it entirely.
7. Publish success or a diagnostic failure.

## Dependencies

### Requires

- Slices 2 and 4.
- Temporary in-memory snapshot storage and entity resolver.

### Enables

- Full save/load, match restart, deterministic test fixtures, and failure diagnostics.

## Integration Boundaries

Grid defines and validates its snapshot section. Match Runtime coordinates save order and profile loading. Unit provides stable identity resolution and restores its own state. Grid never serialises external Module internals.

## Editor and Authoring Support

No content authoring is required. Development controls should capture, inspect, reset, corrupt for negative testing, and restore an in-memory snapshot.

## Debug and Observability

- Snapshot schema/profile fingerprint/occupancy count display.
- Difference view between current occupancy and candidate snapshot.
- Failure message identifying incompatible field or invalid entry.
- Reset/restore lifecycle log.

## Edge Cases and Failure Handling

- Missing profile or unresolved occupant: reject restoration before commit.
- Checksum/version mismatch: reject unless a separately approved migration explicitly supports it.
- Duplicate occupant/cell: reject.
- Visuals absent: logical restore still succeeds; presentation rebuilds later.
- Restore while mutation is in progress: serialise through lifecycle orchestration or reject as busy.

## Acceptance Criteria

- [ ] Capture records profile fingerprint and exact occupancy without visual data.
- [ ] Reset clears all occupancy and preserves layout.
- [ ] Valid restoration recreates identical coordinate-to-identity relationships.
- [ ] Invalid restoration leaves no partial occupancy.
- [ ] Profile mismatch and unresolved identity produce distinct diagnostics.
- [ ] Odd/even position restoration uses logical coordinates and remains visually centred after rebuild.

## Suggested Verification

- **Normal:** place entities, capture, reset, restore, and compare all lookups.
- **Invalid:** corrupt one occupant coordinate and confirm no commit.
- **Boundary:** capture/restore empty and fully occupied deployment states.
- **Integration:** restore Unit-owned placeholder identities, then rebuild Slice 3 visuals.

## Completion State

The Grid's authoritative match state can be reset and reconstructed safely from compact, profile-referenced data.

---
