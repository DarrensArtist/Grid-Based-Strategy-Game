# Grid Module — Codex Task Pack

> **Project:** Grid-Based Strategy Game  
+> **Module:** 01 — Grid System  
+> **Files:** 10 slice task lists  
+> **Recommended use:** Complete, verify, and commit one slice before starting the next.

## Purpose

This pack translates the Grid Module's workable implementation slices into ordered tasks Codex can execute. Each file includes its slice goal, working boundaries, implementation checklist, handoff evidence, Definition of Done, and the full authoritative slice specification.

## Implementation Order

| Slice | Task file | Tasks |
| ---: | --- | ---: |
| 1 | [Logical Coordinates and Cell-Centred World Mapping](01-Logical-Coordinates-and-Cell-Centred-World-Mapping-Tasks.md) | 10 |
| 2 | [Arena Profile Loading and Logical Battlefield Construction](02-Arena-Profile-Loading-and-Logical-Battlefield-Construction-Tasks.md) | 10 |
| 3 | [Authoritative Battlefield Presentation and Basic Grid Diagnostics](03-Authoritative-Battlefield-Presentation-and-Basic-Grid-Diagnostics-Tasks.md) | 10 |
| 4 | [Transactional Cell Occupancy](04-Transactional-Cell-Occupancy-Tasks.md) | 10 |
| 5 | [Reusable Spatial Queries](05-Reusable-Spatial-Queries-Tasks.md) | 10 |
| 6 | [Deployment Zones and Atomic Army Placement](06-Deployment-Zones-and-Atomic-Army-Placement-Tasks.md) | 10 |
| 7 | [Grid Reset, Snapshot, and Restoration](07-Grid-Reset-Snapshot-and-Restoration-Tasks.md) | 10 |
| 8 | [Arena Validation and Gameplay-Ready Certification](08-Arena-Validation-and-Gameplay-Ready-Certification-Tasks.md) | 10 |
| 9 | [Symmetrical Arena Layout Editor — Core Authoring Workflow](09-Symmetrical-Arena-Layout-Editor-Core-Authoring-Workflow-Tasks.md) | 10 |
| 10 | [Arena Editor Presets, Workflow Polish, and Diagnostic Hardening](10-Arena-Editor-Presets-Workflow-Polish-and-Diagnostic-Hardening-Tasks.md) | 10 |

## How to Use Each File with Codex

1. Give Codex the repository plus exactly one slice task file.
2. Ask it to inspect the repository and implement that slice only.
3. Require it to run the listed verification and report handoff evidence.
4. Review the acceptance-criteria checklist before committing.
5. Start the next slice only after required dependencies are working.

## Cross-Slice Rules

- Earlier slices own foundational behaviour; later slices consume it rather than recreate it.
- Grid owns spatial facts, lifecycle, layout, occupancy, queries, snapshots, validation, and Grid-specific tooling only.
- Unit, Action, Turn, Selection, Highlighting, Match Runtime, saving orchestration, and production presentation keep their stated authority.
- Temporary harnesses are allowed when a later module does not yet exist, but they must be replaceable and clearly labelled.
- Atomic operations validate the full candidate before mutating authoritative state.

# Slice Dependency Summary

| Slice | Primary Outcome | Requires | Enables |
|---|---|---|---|
| 1 | Reversible cell-centred coordinate/world mapping | Transform context | All spatial runtime behaviour |
| 2 | Profile-driven logical battlefield | Slice 1 | Visuals, occupancy, queries, validation |
| 3 | Derived Unity presentation and basic diagnostics | Slices 1–2 | Visual verification and later overlays |
| 4 | Atomic one-entity-per-cell occupancy | Slice 2 | Deployment, blockers, snapshots |
| 5 | Deterministic spatial geometry queries | Slice 2; Slice 4 for filters | Actions, highlighting, validation |
| 6 | Zone queries and atomic army placement | Slices 2, 4 | Match Setup deployment flow |
| 7 | Reset, snapshot, and atomic restoration | Slices 2, 4 | Save/load and match restart |
| 8 | Gameplay-ready validation and checksum | Slices 2, 5 | Safe loading and authoring feedback |
| 9 | Core symmetrical visual authoring | Slices 2, 8 | Repeatable bespoke arena creation |
| 10 | Presets, polished workflow, complete diagnostics | Slices 1–9 | Production use and module hand-off |

# Data Ownership Summary

| Data | Owner | May Modify | May Read | Persistence |
|---|---|---|---|---|
| Grid Coordinate value | Grid Module convention | Value is immutable | Any Module | Saved only inside owning state |
| Arena Grid Profile | Grid content/authoring | Approved editor tools/designers | Grid runtime, validation, Match Runtime | Persistent ScriptableObject |
| Profile layout and zones | Arena Grid Profile | Arena Layout Editor | Grid runtime and development tools | Persistent |
| Validation report/status/checksum | Grid validation | Validation process only | Authoring, Match Runtime, build checks | Stored derived metadata where useful |
| Runtime Grid State/cells | Grid Module | Grid lifecycle only | Other Modules through queries | Match lifetime; reconstructed |
| Occupancy and reverse index | Grid Module | Grid transactions only | Unit, Action, Selection, diagnostics | Match state; included in snapshot |
| Unit/entity state | Unit Module or temporary registry | Owning Module | Grid receives opaque identity only | Outside Grid |
| Pending formation | Match Setup | Match Setup/player setup | Grid only when submitted | Outside Grid; policy external |
| Grid Snapshot section | Grid Module; stored by save orchestrator | Grid capture/migration only | Match Runtime and Grid restore | Persistent save data |
| Presentation instances | Grid presenter | Presenter | Human viewer/visual systems | Transient, rebuildable |
| Editor buffer/undo/view state | Arena Layout Editor | Editor session | Authoring UI | Transient, except local preferences |
| Diagnostic state | Grid diagnostics | Developer controls | Development tools | Transient; excluded from match state |

# Integration Summary

## Responsibilities Owned by the Grid Module

- Coordinates, active/inactive topology, zones, cell centres, arena bounds, and profile-driven construction.
- Spatial occupancy and atomic spatial mutations.
- Generic geometry queries.
- Grid reset/snapshot/restore data.
- Arena validation, symmetry, connectivity, authoring, and Grid diagnostics.

## Information Received from Other Modules

- Selected profile and lifecycle requests from Match Runtime.
- Opaque stable entity identities and authorised spatial commands from Unit/Action/Match Setup.
- Complete deployment proposals from Match Setup.
- World points from Selection after it performs camera/pointer raycasting.
- Snapshot orchestration and identity resolver from Save/Match Runtime and Unit.

## Information Exposed to Other Modules

- Cell existence/activity, zones, centres, bounds, profile fingerprint, and occupancy.
- Coordinate/world conversion.
- Ordered spatial-query results.
- Deployment cells/capacities and transaction outcomes.
- Lifecycle, occupancy, reset, and restoration results/notifications.
- Machine-readable validation status.

## Responsibilities Intentionally Deferred

- **Unit:** stats, health, facing, recovery, models, stable gameplay identity, and unit-owned coordinate agreement.
- **Action:** movement/attack legality, distance costs, blockers, line of sight, damage, and action resolution.
- **Selection & Details:** camera raycasting, player input, selected state, and details UI.
- **Grid Highlighting:** normal gameplay highlight appearance/lifetime.
- **Turn & Recovery:** when actions are permitted.
- **Match Setup:** roster rules, pending/private formations, ready state, and phase permission.
- **Match Runtime/Save:** profile selection, lifecycle orchestration, save files/slots, and full-match restoration order.
- **Victory/Army construction:** objectives, legal armies, and match outcome.

# Assumptions

1. The Grid root may be translated and rotated around Unity Y, so conversions use root local/world transforms; non-uniform root scale is not supported initially.
2. World-to-grid uses the nearest containing square on the Grid floor, with a single deterministic tie convention for exact internal boundaries. The precise lower/upper tie choice must be documented during Slice 1.
3. Logical direction ordering and query result ordering are implementation-facing contracts to be selected and frozen in Slice 5; the overview does not specify the order.
4. Connectivity validation initially uses cardinal adjacency because square-cell battlefields normally require shared edges for dependable passage. This remains an assumption until movement topology is confirmed.
5. Generic `groups around a position` support named distance/shape modes rather than one implied universal radius. Only modes required by early Action designs should be implemented.
6. The overview's requirement to place both armies atomically is interpreted as atomicity per complete submitted formation, with Match Runtime coordinating whether both team commits must form one wider transaction.
7. Runtime occupants have stable opaque identifiers supplied by the Unit/entity-owning Module; snapshots store identifiers rather than object references.
8. Layout checksum covers canonical gameplay-affecting profile data and excludes display text, authoring notes, validation timestamps, and runtime state.
9. Minimum/maximum supported dimensions, cell-size bounds, warning thresholds, and snapshot/profile compatibility policy have not yet been confirmed.
10. Runtime presentation may use placeholder meshes/materials; production art remains replaceable and non-authoritative.

# Unresolved Decisions

| Decision | Why It Can Wait | Resolve By |
|---|---|---|
| Exact world-boundary tie rule | Does not change centred mapping architecture if globally consistent | Slice 1 completion |
| Supported Grid-root scaling | Translation/rotation covers the stated requirement | Slice 1 completion |
| Exact profile layout serialisation representation | Behaviour/ownership is defined independently | Slice 2 implementation spec |
| Minimum/maximum dimensions and cell-size bounds | Needed for validation and editor control limits, not core ownership | Before Slice 8 completion; preferably Slice 2 |
| Stable cell identity format | Only determinism and uniqueness are currently required | Slice 2 completion |
| Neighbour/result ordering | Any documented deterministic order works until consumers depend on it | Slice 5 completion |
| Cardinal versus diagonal connectivity for validation | Assumed cardinal; must align with future movement topology | Slice 8 completion |
| Whether origin is included in zero-radius/area queries | Query contract detail | Slice 5 completion |
| Atomicity across both teams together versus per team | Match Setup/Runtime coordination decision | Slice 6 completion |
| Snapshot compatibility and migration policy | Initial snapshots can require exact fingerprint | Before shipped save compatibility; Slice 7 contract |
| Warning thresholds for size, bottlenecks, and neutral depth | Content/balance tuning rather than structural validity | Slice 8 completion |
| Preset algorithms and default sizes | Does not affect core editor workflow | Slice 10 implementation |

# Scope Protection

| Attractive Addition | Exclusion Reason | Correct Home |
|---|---|---|
| A* pathfinding and reachability | Converts geometry into movement interpretation | Action Module/later integration |
| Line of sight, cover, and terrain cost | Requires combat and terrain rules not owned by Grid | Action/environment Modules |
| Multi-cell, stacked, flying, or pushed units | First version explicitly uses one unit per cell | Future Grid/Unit extension |
| Hexes, triangles, or multi-level grids | Conflicts with confirmed flat square-cell foundation | Possible future extension |
| Procedural competitive arena generation | Presets are only editable starting points | Future content tooling |
| Automatic arena balancing by simulation | Validation establishes structural suitability, not meta balance | Future analytics/polish |
| Formation UI, privacy, and ready state | Grid only validates submitted spatial proposals | Match Setup Module |
| Movement/attack highlighting | Grid must not own normal gameplay highlighting | Grid Highlighting Module |
| Camera raycasting and selection | Grid only converts a supplied world position | Selection & Details Module |
| Production floor/wall/banner authoring | Visual boundaries cannot become logical authority | Environment/art polish pass |
| Network replication and rollback | No networking requirement is specified | Future runtime architecture |
| Universal save framework | Grid owns only its snapshot section | Match Runtime/Save system |
| Runtime editing of competitive profiles | Risks configuration/state ownership and certification | Future controlled tool, if required |

---

## Final Implementation Outcome

After all ten slices, the project can author, validate, load, display, query, occupy, deploy onto, reset, and restore reusable symmetrical square-cell arenas. All unit-facing positions use cell centres; odd and even dimensions remain correctly centred; inactive coordinates behave as nonexistent space; spatial mutations are atomic; configuration remains separate from live match state; and later Modules can consume clear read-only spatial facts without the Grid absorbing their gameplay responsibilities.
