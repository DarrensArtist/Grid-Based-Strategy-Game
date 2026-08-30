# Grid Module — Slice 10 Codex Task List

> **Slice:** Arena Editor Presets, Workflow Polish, and Diagnostic Hardening  
+> **Recommended order:** 10 of 10  
+> **Status:** Not started  
+> **Source:** Grid Module — Workable Implementation Slices

## Goal

Complete the internal arena tool with reusable starting presets, high-quality navigation/safety behaviour, layered diagnostics, and end-to-end profile verification.

## Codex Working Contract

- Work only within this slice and its declared integration seams. Do not pre-implement later modules.
- Inspect the repository, `AGENTS.md`, Unity version, assembly definitions, tests, and existing conventions before changing code.
- Preserve user changes and avoid unrelated refactors.
- Prefer small, testable types with explicit failure results over hidden fallbacks.
- Treat logical Grid state as authoritative; presentation objects never become gameplay truth.
- Run the narrowest relevant tests after each meaningful group of changes, then run the full Grid test set before handoff.
- If the repository contradicts a required design rule, stop and report the conflict instead of silently changing the rule.

## Prerequisites

Complete all required earlier slices stated in the source specification; normally begin after Slice 9 is verified.

## Ordered Implementation Tasks

- [ ] **Task 1: Implement editable starting presets** — Add rectangle, taper, octagon, and grid-approximated circle generators that populate the editing buffer and remain fully editable.
- [ ] **Task 2: Harden view navigation** — Add zoom-to-fit, reset view, coordinate options, scene focus, remembered non-gameplay view preferences, and robust resizing/scrolling.
- [ ] **Task 3: Polish safety and discoverability** — Audit tooltips, disabled reasons, confirmation wording, selection feedback, unsaved state, and keyboard/mouse behaviour.
- [ ] **Task 4: Consolidate layered diagnostics** — Provide individually controlled active/inactive, zones, occupancy, boundaries, queries, symmetry, connectivity, metadata, snapshots, and lifecycle views with uncluttered defaults.
- [ ] **Task 5: Add end-to-end fixture catalogue** — Cover valid and invalid profiles, initialisation, presentation, occupancy, queries, deployment, snapshot/restore, validation, editing, and presets.
- [ ] **Task 6: Automate integration verification** — Create edit-mode/play-mode tests that load authored/preset profiles and exercise the full Grid responsibility chain.
- [ ] **Task 7: Check performance and cleanup** — Measure representative large arenas, avoid leaked editor/runtime objects or subscriptions, and keep expensive labels/diagnostics optional.
- [ ] **Task 8: Audit assembly and build separation** — Ensure editor-only code and assets do not enter player assemblies and runtime diagnostics default off.
- [ ] **Task 9: Run regression suite** — Compile and run all Grid tests, then resolve failures without widening Grid ownership into other modules.
- [ ] **Task 10: Produce final module handoff** — Document public APIs, profile authoring workflow, diagnostics, known limits, test commands, and readiness for Unit/Action/Match integration.

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
