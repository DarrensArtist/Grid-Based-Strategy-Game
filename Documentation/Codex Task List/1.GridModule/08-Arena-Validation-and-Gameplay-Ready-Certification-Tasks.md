# Grid Module — Slice 8 Codex Task List

> **Slice:** Arena Validation and Gameplay-Ready Certification  
+> **Recommended order:** 8 of 10  
+> **Status:** Not started  
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Evaluate an Arena Grid Profile against structural, fairness, connectivity, and playability rules and certify it Gameplay Ready only when all blocking checks pass.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

Complete all required earlier slices stated in the source specification; normally begin after Slice 7 is verified.

## Ordered Implementation Tasks

- [ ] **Task 1: Define validation report models** — Represent ordered errors/warnings, rules, affected coordinates/regions, explanations, remedies, summaries, checksum, and certification state.
- [ ] **Task 2: Implement canonical checksum input** — Include layout-affecting configuration deterministically and exclude notes, timestamps, runtime state, and other irrelevant fields.
- [ ] **Task 3: Implement structural and metadata checks** — Validate dimensions, cell size, payloads, centre handling, zones, counts, and derived metadata consistency.
- [ ] **Task 4: Implement rotational symmetry checks** — Use (width-1-x, height-1-z), including odd self-paired centres and Team A/Team B/Neutral/Inactive counterpart rules.
- [ ] **Task 5: Implement capacity and zone checks** — Require equal, non-empty deployment areas and prevent neutral deployment assignments.
- [ ] **Task 6: Implement connectivity analysis** — Use one documented neighbour model to identify components, isolated active cells, and whether a component connects both deployment sides.
- [ ] **Task 7: Implement certification and staleness** — Mark Gameplay Ready only for the exact version/checksum with zero blocking errors; invalidate it when relevant data changes.
- [ ] **Task 8: Add editor/runtime gates** — Expose machine-readable results and prevent manual certification or competitive loading of stale/uncertified profiles.
- [ ] **Task 9: Create focused fixtures** — Add valid odd/even shapes and one failing fixture per blocking rule, plus warning-only cases.
- [ ] **Task 10: Verify determinism and explanations** — Run repeated validation, compare checksums/order, and ensure each failure identifies an actionable location and correction.

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

# Slice 8: Arena Validation and Gameplay-Ready Certification

## Goal

Evaluate an Arena Grid Profile against structural, fairness, connectivity, and playability rules and certify it Gameplay Ready only when all blocking checks pass.

## Player or Developer Value

This content-authoring and debugging slice prevents invalid arenas from reaching matches and explains how to correct them rather than merely reporting failure.

## In-Scope Behaviour

- Validate dimensions, cell size, centre handling, active/zone data, and metadata consistency.
- Verify 180-degree rotational symmetry of active state and opposing deployment zones.
- Verify equal deployment capacity and non-empty deployment zones.
- Ensure neutral cells are not deployment cells.
- Detect disconnected active regions and isolated cells.
- Verify at least one active route connects both deployment sides through the battlefield.
- Produce blocking errors and non-blocking warnings with locations and suggested corrections.
- Calculate active count, deployment capacity, validation status, and deterministic layout checksum.
- Mark Gameplay Ready only for the exact unchanged layout that passed.

## Out of Scope

- Balancing movement/attack meta, guaranteeing fun, simulating units, or assessing race matchups.
- Automatically repairing profiles without explicit author action.
- Runtime Action pathfinding.

## Game Behaviour

This is primarily an authoring/runtime gate. A developer requests validation after editing. Checks operate on profile configuration and produce a report. Blocking findings prevent Gameplay Ready. Warnings remain visible but do not block. Any layout-affecting change invalidates the prior certification until validation runs again.

## Logical Rules

1. Rotational counterpart of `(x, z)` is `(width - 1 - x, height - 1 - z)`.
2. Active state must match its rotational counterpart.
3. Team A deployment must rotate to Team B deployment and vice versa.
4. Neutral rotates to neutral; inactive rotates to inactive.
5. Both deployment zones must be non-empty and equal in capacity.
6. Connectivity uses one explicitly documented neighbour model; diagonal-only contact is not silently treated as connected unless confirmed.
7. Isolated active cells are blocking even if another route exists.
8. At least one connected active component must contain both teams' deployment regions.
9. Errors block Gameplay Ready; warnings do not.
10. Certification is bound to profile version/checksum and becomes stale after relevant edits.
11. Checksum is deterministic for canonical layout/configuration data and excludes notes, validation timestamps, and runtime state.

## State and Data

- **Validation report:** derived authoring data owned by Grid validation; may be stored with profile for inspection but is regenerated.
- **Gameplay Ready status/checksum:** persistent derived certification associated with the profile.
- **Affected coordinates/regions:** report details for editor focus.
- **Thresholds for warnings:** project configuration or documented defaults; changing them must not silently alter blocking geometry rules.

## Inputs

- Arena Grid Profile configuration.
- Validation request from editor, asset pipeline, test, or runtime safety gate.

## Outputs

- Ordered errors and warnings with rule, affected area, explanation, and suggested remedy.
- Derived counts, symmetry/connectivity status, checksum, and Gameplay Ready flag.
- Machine-readable validation result for build/runtime gates.

## System Flow

1. Canonicalise/read profile data without mutating its layout.
2. Run structural checks; stop dependent checks when structure is unreadable but continue independent diagnostics.
3. Run zone and rotational symmetry checks.
4. Run connectivity, isolation, and cross-arena route checks.
5. Calculate summaries/checksum.
6. Publish errors/warnings.
7. Certify the exact version/checksum only when there are zero blocking errors.

## Dependencies

### Requires

- Slice 2 profile semantics and Slice 5 neighbour geometry.

### Enables

- Safe match loading, efficient custom authoring, profile catalogues, and build validation.

## Integration Boundaries

Validation may reuse Grid topology operations but does not add Action rules. Match Runtime checks certification before competitive use; it does not reinterpret the report.

## Editor and Authoring Support

- Validation command and status badge.
- Error/warning list with coordinate focus and human-readable fixes.
- Summary counts and stale-validation indicator.
- Protection against manually setting Gameplay Ready.

## Debug and Observability

- Overlays for symmetry pairs, disconnected components, isolated cells, route result, and offending zones.
- Inspect checksum inputs at a high level and compare last-certified/current checksum.
- Automated fixture report for each validation rule.

## Edge Cases and Failure Handling

- Empty arena or zone: blocking error.
- Odd arena centre: central cell maps to itself and must have a self-consistent state.
- Even arena centre: central cells pair correctly under rotation.
- Multiple valid components: disconnected regions remain blocking per overview.
- Very narrow but connected route: warning if below threshold, unless isolated/disconnected.
- Editing only notes: does not invalidate layout checksum, though other asset dirty state may change.

## Acceptance Criteria

- [ ] Every listed blocking condition has an automated failing fixture.
- [ ] Valid odd and even symmetrical profiles pass.
- [ ] One asymmetric cell identifies both it and its expected counterpart.
- [ ] Disconnected components, isolated cells, and absent cross-arena route are reported distinctly.
- [ ] Warnings do not block Gameplay Ready.
- [ ] A layout edit makes prior certification stale.
- [ ] Revalidating identical canonical data produces the same checksum.

## Suggested Verification

- **Normal:** validate rectangle, taper, octagon, and stepped-circle fixtures.
- **Invalid:** test one fixture per blocking rule.
- **Boundary:** test central cells in minimum supported odd/even arenas.
- **Integration:** reject competitive loading of an uncertified or checksum-stale profile.

## Completion State

Arena profiles have an explainable, deterministic gate between editable content and match-ready battlefield geometry.

---
