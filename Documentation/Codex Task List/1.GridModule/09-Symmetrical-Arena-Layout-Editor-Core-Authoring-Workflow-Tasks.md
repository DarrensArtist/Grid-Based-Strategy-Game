# Grid Module — Slice 9 Codex Task List

> **Slice:** Symmetrical Arena Layout Editor — Core Authoring Workflow  
+> **Recommended order:** 9 of 10  
+> **Status:** Not started  
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Provide a dedicated Unity Editor window for creating and editing one side and the neutral centre of a symmetrical Arena Grid Profile without typing arrays or coordinates.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

Complete all required earlier slices stated in the source specification; normally begin after Slice 8 is verified.

## Ordered Implementation Tasks

- [ ] **Task 1: Create the dedicated editor window shell** — Build resizable/scrollable profile-tools, Grid canvas, and properties/validation panels usable at 1920x1080.
- [ ] **Task 2: Implement profile lifecycle controls** — Create, select, duplicate, save, revert, and detect externally changed assets without silent overwrite.
- [ ] **Task 3: Create an isolated editing buffer** — Keep unsaved candidate configuration separate from runtime Grid state and source assets until save.
- [ ] **Task 4: Implement pan/zoom canvas rendering** — Display coordinates, centre, source-editable cells, generated mirror cells, neutral cells, zones, and selected-cell details.
- [ ] **Task 5: Implement symmetrical editing rules** — Allow Team A source and neutral edits, generate Team B by 180-degree rotation, and prevent contradictory direct edits.
- [ ] **Task 6: Implement drawing tools** — Support single paint/erase, rectangle, fill, clear, and invert while restricting each gesture to permitted source regions.
- [ ] **Task 7: Add coherent undo/redo** — Record each drawing gesture or confirmed structural change as one undoable operation.
- [ ] **Task 8: Protect destructive geometry changes** — Preview impact and require confirmation for dimension or neutral-depth changes that discard or reclassify data.
- [ ] **Task 9: Integrate validation and save semantics** — Invalidate stale certification on layout save, run Slice 8 validation, and focus affected canvas regions.
- [ ] **Task 10: Test authoring workflows** — Cover odd/even centres, generated-cell protection, neutral symmetry, close-dirty choices, external conflicts, duplicate independence, save/load, and runtime compatibility.

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
