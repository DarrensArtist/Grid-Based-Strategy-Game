# Grid Module — Slice 6 Codex Task List

> **Slice:** Deployment Zones and Atomic Army Placement  
+> **Recommended order:** 6 of 10  
+> **Status:** Not started  
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Expose deployment capacity and atomically apply a complete team's proposed starting formation within its permitted zone.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

Complete all required earlier slices stated in the source specification; normally begin after Slice 5 is verified.

## Ordered Implementation Tasks

- [ ] **Task 1: Define deployment query and transaction types** — Represent team, proposal entries, replacement intent, per-entry errors, capacity, and atomic results without storing private pending formations.
- [ ] **Task 2: Expose zone and capacity queries** — Return each team deployment cells, neutral cells, total capacity, and currently free capacity.
- [ ] **Task 3: Validate complete proposals** — Check lifecycle, team, unique identities, unique destinations, activity, correct zone, capacity, and occupancy for every entry before mutation.
- [ ] **Task 4: Build an atomic occupancy candidate** — Reuse Slice 4 authority while ensuring no proposal can partially place an army.
- [ ] **Task 5: Commit full deployments** — Apply all entries together and publish notifications only after the entire transaction is consistent.
- [ ] **Task 6: Implement explicit replace/clear flow** — Allow setup-phase replacement as one transaction, treating the team’s existing entries correctly without opening general swap behaviour.
- [ ] **Task 7: Return actionable failures** — Preserve existing state and identify failing entries wherever possible.
- [ ] **Task 8: Add setup-facing test harness** — Submit formations without building final UI, roster legality, secrecy, networking, or turn rules.
- [ ] **Task 9: Test normal and adversarial proposals** — Cover full zones, duplicates, wrong side, neutral/inactive cells, occupied cells, insufficient capacity, replacement, and both teams.
- [ ] **Task 10: Verify boundaries and integration** — Confirm pending formations remain external and all committed presence agrees with Slice 4 lookups.

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

# Slice 6: Deployment Zones and Atomic Army Placement

## Goal

Expose deployment capacity and atomically apply a complete team's proposed starting formation within its permitted zone.

## Player or Developer Value

This player-facing integration slice lets each player arrange units on their own side and guarantees that a failed formation cannot leave a partially deployed army.

## In-Scope Behaviour

- Enumerate each team's active deployment cells and neutral cells.
- Report zone capacity and current free capacity.
- Validate a proposed set of entity-to-coordinate placements.
- Enforce correct team zone, active cells, unique entities, unique destinations, and unoccupied destinations.
- Commit the complete proposal atomically through occupancy.
- Remove or replace a team's deployment during the setup phase through an explicit transaction.
- Keep private formation content outside Runtime Grid State until committed.

## Out of Scope

- Formation editing UI, ready states, secrecy/network replication, roster legality, unit ownership rules, turn start, and deployment timing.
- Movement restrictions after combat begins.
- Choosing default formations.

## Game Behaviour

Match Setup maintains a player's pending formation. When asked to commit it, Grid receives opaque entity identifiers, the requesting team, and destinations. It checks the entire proposal. If one entry fails, nothing is placed. If all pass, all occupancy changes become visible together. No Man's Land and the opposing deployment side are always invalid starting destinations.

## Logical Rules

1. Team A proposals may use only Team A deployment cells; Team B likewise.
2. Neutral and inactive cells cannot be deployment destinations.
3. Every proposed entity and coordinate must be unique.
4. Every destination must be empty or explicitly part of the same replacement transaction.
5. Proposal count cannot exceed available deployment capacity.
6. All entries validate before any occupancy change.
7. Failure returns entry-specific reasons where possible and preserves previous occupancy.
8. Success uses the same occupancy authority as individual placement.
9. Grid does not decide whether the roster or phase permits the request; Match Setup gates the call.
10. Both teams use the same loaded profile; symmetry validation ensures geometric fairness.

## State and Data

- **Zone assignments:** profile-derived layout, Grid-owned and persistent through the profile.
- **Committed occupancy:** match state owned by Grid and snapshot-eligible.
- **Pending private formation:** owned by Match Setup/player setup, never stored in the Arena Grid Profile or Runtime Grid State.
- **Deployment proposal/result:** transient transaction data.

## Inputs

- Team identifier and formation proposal from Match Setup.
- Opaque entity identities from Unit/roster setup.
- Explicit replace/clear operation from Match Setup when allowed.

## Outputs

- Zone cell/capacity queries to setup UI and Match Setup.
- Atomic success/failure result with per-entry diagnostics.
- Post-commit occupancy notifications.

## System Flow

1. Match Setup submits a complete proposal.
2. Grid validates lifecycle state, team, duplicates, zones, activity, and occupancy for every entry.
3. On any failure, return reasons and perform no mutation.
4. On success, create the complete candidate occupancy change.
5. Commit it atomically and publish notifications after consistency is restored.
6. Setup UI reflects the authoritative committed result.

## Dependencies

### Requires

- Slices 2 and 4.
- A temporary Match Setup harness and test entity identities.

### Enables

- Real pre-match formation flow and fair army setup.

## Integration Boundaries

Match Setup owns proposals, readiness, privacy, and phase permission. Unit owns entity/team facts and should provide already-authorised identities. Grid owns only spatial validation and committed occupancy.

## Editor and Authoring Support

Arena profile summaries show deployment capacity per team. A play-mode harness can construct proposals and display entry-level rejection reasons.

## Debug and Observability

- Deployment-zone overlay and capacity counts.
- Proposal preview separate from committed occupancy.
- Transaction log showing validation order, failures, and whether a commit occurred.

## Edge Cases and Failure Handling

- Empty proposal: allowed only when setup rules deliberately permit an empty army; otherwise caller rejects before Grid.
- Duplicate entity/destination: reject whole proposal.
- Another team already occupies destination: reject.
- Replacement transaction fails: preserve the previous committed formation.
- Profile has zero deployment cells: validation prevents Gameplay Ready status; runtime proposal fails safely.

## Acceptance Criteria

- [ ] Each zone query returns the correct active cells and capacity.
- [ ] A valid multi-unit proposal appears completely.
- [ ] One invalid entry prevents all proposal placements.
- [ ] Neutral, enemy, inactive, outside, duplicate, and occupied destinations are rejected distinctly.
- [ ] Pending formation data is absent from Grid runtime/profile data.
- [ ] Team A and B capacity is equal for a valid symmetrical profile.

## Suggested Verification

- **Normal:** commit valid formations for both teams.
- **Invalid:** include one neutral destination in a five-unit proposal and confirm zero new occupants.
- **Boundary:** fill every deployment cell exactly, then attempt one extra entity.
- **Integration:** have a temporary setup controller retain/edit its proposal after Grid rejection.

## Completion State

The Grid can safely receive and atomically place complete armies while formation and match-flow responsibilities remain external.

---
