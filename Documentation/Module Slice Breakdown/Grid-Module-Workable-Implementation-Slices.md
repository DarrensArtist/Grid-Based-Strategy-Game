# Grid Module — Workable Implementation Slices

> **Project:** Grid-Based Strategy Game  
> **Module:** 01 — Grid System  
> **Source:** Grid Module Descriptive Overview  
> **Document type:** Ordered implementation plan  
> **Status:** Draft for implementation review

## Analysis Summary

The Grid Module is the authoritative spatial foundation for each match. It owns reusable arena geometry, logical cells, coordinate/world conversion, deployment regions, occupancy, spatial queries, restoration data, validation, authoring, and Grid-specific diagnostics. It exposes spatial facts while deliberately leaving movement permission, attack interpretation, unit statistics, selection, turns, highlighting, formations, and victory rules to their own Modules.

The implementation is divided into ten functional slices. Each slice produces behaviour that can be tested without completing the rest of the game. Temporary test entities and simple development visuals are used until the Unit, Match Setup, Selection, Highlighting, Action, and Match Runtime Modules are available.

---

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

- [ ] A 9×9 grid maps `(4, 4)` to the Grid root.
- [ ] A 10×10 grid maps its central four cells to `±0.5 × cell size` on both local axes.
- [ ] Every valid test coordinate round-trips through its centre.
- [ ] Invalid coordinates and outside world points return failure without a usable fabricated result.
- [ ] Moving or rotating the Grid root changes world results but not logical coordinates.
- [ ] No public operation requires a caller-applied half-cell offset.

## Suggested Verification

- **Normal:** round-trip all coordinates in a 9×9 grid.
- **Invalid:** try zero cell size and coordinate `(-1, 0)`; both are rejected with distinct reasons.
- **Boundary:** test all cells of a 10×10 grid and points on outer/internal edges.
- **Integration:** place a marker at a returned centre under a rotated root and convert its Transform position back.

## Completion State

The project has one authoritative, reversible convention for logical coordinates and cell-centred world placement.

---

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

- [ ] A valid profile creates the expected number of backing entries and active cells.
- [ ] Every active cell reports exactly one correct zone.
- [ ] Inactive coordinates are rejected by playable-cell queries.
- [ ] Runtime mutation does not change the source asset.
- [ ] Invalid profile construction publishes no partial ready state.
- [ ] Profile identity, version, and checksum are inspectable at runtime.

## Suggested Verification

- **Normal:** load a small symmetrical fixture with all three regions and compare every cell.
- **Invalid:** load a profile whose payload size differs from its dimensions.
- **Boundary:** load minimum supported odd and even dimensions.
- **Integration:** ask the Slice 1 mapper for centres of active corner cells from loaded dimensions.

## Completion State

The project can load reusable arena configuration and expose a complete authoritative logical battlefield.

---

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

- [ ] Only active cells receive visible playable surfaces.
- [ ] Region colours match authoritative zone data.
- [ ] All surfaces are centred through Grid conversion.
- [ ] Moving a visual manually does not alter Grid state.
- [ ] Rebuilding produces the same footprint and cell identities.
- [ ] Diagnostic layers can be enabled separately and are disabled by default at runtime.

## Suggested Verification

- **Normal:** display a cut-corner profile with three regions.
- **Invalid:** attempt presentation before initialisation and observe a useful message rather than errors.
- **Boundary:** display odd and even profiles and inspect their centres.
- **Integration:** move/rotate the Grid root, rebuild, and confirm alignment.

## Completion State

The logical Grid can be seen and inspected in Unity without surrendering authority to scene objects.

---

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

- [ ] Valid placement updates both lookup directions.
- [ ] Invalid placement changes neither lookup.
- [ ] Valid movement frees the source and occupies the destination atomically.
- [ ] Failed movement leaves the occupant at its original cell.
- [ ] Removal frees the cell and reverse index.
- [ ] Inactive/outside coordinates never accept occupants.
- [ ] Consistency scan finds no mismatch after every supported operation sequence.

## Suggested Verification

- **Normal:** place, move, query, and remove one test entity.
- **Invalid:** move into an occupied cell and compare state before/after.
- **Boundary:** use active cells adjacent to inactive/outside coordinates.
- **Integration:** update a placeholder entity's visual only after a success notification.

## Completion State

The Grid reliably owns spatial occupancy without owning the entities or their gameplay permissions.

---

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

# Slice 9: Symmetrical Arena Layout Editor — Core Authoring Workflow

## Goal

Provide a dedicated Unity Editor window for creating and editing one side and the neutral centre of a symmetrical Arena Grid Profile without typing arrays or coordinates.

## Player or Developer Value

This content-authoring slice makes bespoke arena production practical, repeatable, and safe for designers while enforcing the game's symmetry model at the point of creation.

## In-Scope Behaviour

- Create, select, and duplicate profiles.
- Edit backing-grid dimensions and No Man's Land depth through labelled controls.
- Display a large pan/zoom Grid canvas with centre and coordinate labels.
- Allow Team A-side activation/deactivation, rectangle paint/erase, fill, clear, and invert.
- Generate Team B through live 180-degree rotation rather than independent editing.
- Edit the neutral centre while preserving symmetry and neutral-only zoning.
- Preview the complete arena and derived zone assignments.
- Integrate undo/redo, dirty state, save, revert, and validation.
- Protect generated opposing cells from direct contradictory editing.

## Out of Scope

- Preset library and advanced workflow polish; Slice 10.
- Production scene art editing.
- Manual Team B geometry that breaks symmetry.
- Occupancy, units, formations, gameplay highlights, or runtime match state.

## Game Behaviour

This slice is developer-facing. The author opens the window, selects a profile, defines dimensions/neutral depth, and paints the editable side. Every edit previews its rotational counterpart. Neutral edits automatically apply any required rotational partner and cannot become deployment cells. Saving writes configuration only; validation determines Gameplay Ready.

## Logical Rules

1. Team B layout is derived from Team A via the Slice 8 rotation rule.
2. Generated opposing cells cannot be directly edited as independent source data.
3. Neutral edits preserve rotational symmetry, including self-mapped central cells.
4. Changing dimensions or neutral depth shows the effects and requires confirmation when data would be discarded/reclassified.
5. All drawing gestures are undoable coherent operations.
6. Unsaved edits are visually indicated.
7. Save writes canonical profile layout and invalidates stale certification before revalidation.
8. Revert restores the last saved asset state.
9. Closing with changes offers save, discard, or cancel.
10. Destructive clear/invert/dimension operations require confirmation proportional to data loss.

## State and Data

- **Editing buffer:** transient editor-owned candidate configuration; never used as live match state.
- **Selected profile:** persistent asset owned by Grid content.
- **Canvas view state:** editor preference, not profile gameplay data.
- **Undo records and dirty state:** editor session data.
- **Generated mirror preview:** derived, never a competing authoritative layout.

## Inputs

- Mouse/keyboard drawing and navigation.
- Profile asset selection and creation/duplication commands.
- Geometry/No Man's Land field changes.
- Save/revert/validate/preview commands.

## Outputs

- Updated Arena Grid Profile configuration on save.
- Complete mirrored preview and selected-cell details.
- Validation requests/reports from Slice 8.
- Focus request to the Scene diagnostic view where supported.

## System Flow

1. Create/select a profile and load it into an editing buffer.
2. Display source-editable, generated, and neutral regions distinctly.
3. Apply a drawing gesture to allowed source cells.
4. Generate rotational counterpart(s) and refresh summaries.
5. Mark changes dirty and add one undo operation.
6. Preview/validate repeatedly without committing runtime state.
7. Save canonical configuration or revert/discard safely.

## Dependencies

### Requires

- Slices 2 and 8; Slice 3 concepts for preview.

### Enables

- Rapid creation of approved bespoke arena profiles and Slice 10 workflow polish.

## Integration Boundaries

The window edits only Grid profile configuration. Runtime Grid loading remains Slice 2. Scene focus and preview are read-only bridges; the editor never creates Unit/Action data.

## Editor and Authoring Support

The dedicated window contains:

- **Profile/tools panel:** selected asset, create/duplicate, dimensions, No Man's Land, tools, save/revert, validate, preview.
- **Grid canvas:** pan/zoom, cell states, source/generated/neutral regions, labels, mirror preview, arena centre.
- **Properties/validation panel:** profile summary, selected cell, counts, symmetry/connectivity, errors/warnings.

Controls use human-readable titles, tooltips, clear disabled reasons, consistent spacing, and 1080p-compatible resizable/scrollable panels.

## Debug and Observability

- Hover/selection displays coordinate, rotational partner, source role, active state, and derived zone.
- Preview can colour source versus generated data.
- Validation messages focus the affected coordinate/region.
- A compact change summary shows counts added, removed, or reclassified since save.

## Edge Cases and Failure Handling

- Dimension shrink would remove cells: show impact and require confirmation.
- Odd central cell maps to itself: edit once and display as self-paired neutral where applicable.
- Fill encounters generated region: affect only permitted source region.
- Asset changed externally: detect conflict and offer reload/cancel rather than overwrite silently.
- Window closes dirty: save/discard/cancel.

## Acceptance Criteria

- [ ] A designer can create a new profile without editing raw arrays.
- [ ] Painting one source cell immediately previews the correct rotational counterpart.
- [ ] Generated Team B cells cannot be edited independently.
- [ ] Neutral cells remain neutral and symmetrical.
- [ ] Every drawing tool supports undo/redo.
- [ ] Save/revert/close behaviours protect unsaved work.
- [ ] The window remains usable at 1920×1080 with panel resizing/scrolling.
- [ ] Saved profiles load through Slice 2 and validate through Slice 8.

## Suggested Verification

- **Normal:** author a taper from a blank profile, validate, save, and load it at runtime.
- **Invalid:** deliberately create disconnected source regions and follow validation focus.
- **Boundary:** edit odd/even centres and shrink dimensions with data at removed edges.
- **Integration:** duplicate a profile, change the copy, and prove the original/runtime data remain unchanged.

## Completion State

Designers can author safe symmetrical arenas through a purpose-built visual workflow rather than code or raw coordinate data.

---

# Slice 10: Arena Editor Presets, Workflow Polish, and Diagnostic Hardening

## Goal

Complete the internal arena tool with reusable starting presets, high-quality navigation/safety behaviour, layered diagnostics, and end-to-end profile verification.

## Player or Developer Value

This content-authoring and debugging slice turns the functional editor into a dependable repeated-use production tool and makes Grid failures explainable across authoring, loading, queries, occupancy, and restoration.

## In-Scope Behaviour

- Apply editable starting presets for rectangle, taper, octagon, and grid-approximated circle.
- Zoom to fit, reset view, coordinate display options, Scene focus, and resizable/scrollable panels.
- Clear disabled states, confirmation flows, and helpful tooltips across all controls.
- Layered Scene/runtime diagnostics for active/inactive state, zones, occupancy, boundaries, spatial queries, symmetry, connectivity, profile metadata, snapshots, and lifecycle failures.
- Individual visibility controls and uncluttered default views.
- Automated end-to-end fixtures covering valid/invalid profiles and all earlier Grid responsibilities.
- Build safeguards excluding editor-only tools and disabling runtime debug display by default.

## Out of Scope

- Production environment art tools.
- Procedural balance optimisation or automatic generation of competitive arenas.
- Networking, replay, AI, Action rules, or Highlighting implementation.
- Multi-level or non-square grids.

## Game Behaviour

Presets give authors a starting silhouette, not a locked template. Applying one previews its impact and produces ordinary editable source data. Diagnostics can explain a selected cell, query, mutation, validation failure, or restore failure from the same authoritative Grid facts. Enabling them never changes gameplay.

## Logical Rules

1. Presets obey current dimensions/symmetry or clearly explain incompatible inputs.
2. Applying a preset is one undoable, confirmable operation when it replaces work.
3. Preset output becomes normal editable profile data with no runtime dependency on the preset.
4. Every diagnostic layer reads authoritative state only.
5. Diagnostic visibility cannot mutate layout, zones, occupancy, queries, or lifecycle.
6. Editor-only code/assets are excluded from player builds.
7. Runtime debug overlays default off and require explicit development enablement.
8. Diagnostic output identifies input, applied rule, result, and failure reason where applicable.
9. End-to-end tests use logical state assertions; visual checks supplement rather than replace them.

## State and Data

- **Preset definitions:** editor configuration owned by Grid authoring; persistent but not referenced by runtime profiles after application.
- **Editor view preferences:** local development preferences, not gameplay/save data.
- **Diagnostic selections/history:** transient development state.
- **Test fixtures:** development assets/data with known expected outcomes.

## Inputs

- Preset and navigation commands from the author.
- Authoritative runtime/profile/snapshot/query information from earlier slices.
- Build configuration and automated test runner.

## Outputs

- Editable preset-generated layout.
- Focused layered visual diagnostics and structured failure reports.
- Automated verification results and build-time editor-code safeguards.

## System Flow

1. Author chooses a preset and sees affected dimensions/region summary.
2. On confirmation, tool records undo and writes preset output to the editing buffer.
3. Author edits, previews, validates, and saves normally.
4. During runtime/development diagnosis, choose one or more independent layers.
5. Tools read the relevant authoritative state and explain the result.
6. Automated suites load fixtures, run operations, and compare exact outputs.
7. Build validation confirms editor tooling is excluded and runtime overlays default off.

## Dependencies

### Requires

- All previous slices, especially Slices 8 and 9.

### Enables

- Efficient arena content production and confident hand-off to Unit, Action, Selection, Highlighting, Match Setup, Save, and Match Runtime work.

## Integration Boundaries

Diagnostics may subscribe to public results/events but cannot reach into and modify another Module. Future Modules may contribute their own overlays; Grid diagnostics continue to show spatial facts only.

## Editor and Authoring Support

- Preset gallery/list with short silhouette description and preview.
- Zoom-to-fit and reset-view controls always reachable.
- Tooltips explain effects and disabled reasons.
- Destructive actions report what will change.
- Diagnostic layer menu groups layout, zones, occupancy, queries, validation, lifecycle, and snapshots.

## Debug and Observability

This slice completes the overview's diagnostic set: coordinates, identities, centres, active/inactive cells, zones, occupancy, boundaries, query rays/regions, profile fingerprint, symmetry pairs, connected components, snapshot facts, and load/restore failures. Each can be independently hidden.

## Edge Cases and Failure Handling

- Tiny dimensions cannot express a preset: disable with a reason or propose valid dimensions; never write malformed output.
- Preset replaces dirty work: require confirmation and retain undo.
- Many labels overlap: offer zoom threshold and selective layers.
- Missing entity resolution: display saved opaque identity and failure without inventing a live object.
- Release build: editor references cause build validation failure rather than leaking tooling.

## Acceptance Criteria

- [ ] All four suggested presets create symmetrical, editable starting layouts.
- [ ] Applying/reverting a preset is undoable and cannot silently erase dirty work.
- [ ] Navigation and panels remain comfortable on a 1080p display.
- [ ] Every diagnostic category can be toggled independently.
- [ ] Diagnostics explain why representative query, occupancy, validation, and restore operations succeeded or failed.
- [ ] Enabling diagnostics does not change authoritative state.
- [ ] Editor-only tools are absent from release builds and runtime overlays start disabled.
- [ ] End-to-end tests cover profile load, display derivation, occupancy, queries, deployment, reset/restore, validation, and authored-profile reuse.

## Suggested Verification

- **Normal:** apply each preset, edit it, validate it, save it, and load it into a runtime fixture.
- **Invalid:** apply a too-large/incompatible preset and observe a protected, explanatory result.
- **Boundary:** use minimum/large supported dimensions and dense diagnostic layers.
- **Integration:** run the complete suite in editor and player-test configurations, confirming identical logical results and correct tool exclusion.

## Completion State

The Grid Module has a reusable content pipeline, explainable diagnostics, and verified integration boundaries suitable for later gameplay Modules.

---

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
