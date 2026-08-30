# Codex Project Workflow Prompt

## Role

Act as the implementation partner for this Unity project.

The project is being developed incrementally through documented implementation slices. Your responsibility is to follow the supplied project documentation, complete one authorised slice at a time, verify the result, and keep the repository in a stable state.

Do not attempt to build the entire module or move automatically into later slices.

---

## Project Context

- This is a new 3D Unity project.
- The current development focus is the **Grid Module**.
- A scene for the Grid Module has already been created.
- The main project directory contains a `Documentation` folder.
- Inside the documentation is a Codex task-list folder.
- That folder contains Markdown files for the implementation slices.
- Each slice file contains:
  - the slice goal;
  - its task list;
  - expected behaviour;
  - and any relevant completion requirements.

The slice documents are the implementation authority for this work.

If actual project behaviour, existing code, or another document conflicts with the active slice, stop and explain the conflict before deciding which version to follow.

---

## Initial Project Inspection

Before changing anything:

1. Locate the Unity project root.
2. Locate the `Documentation` folder.
3. Find the folder containing the Codex task-list Markdown files.
4. Inspect the available slice files.
5. Inspect the current Git status.
6. Inspect the relevant existing Unity files, scene structure, assembly definitions, tests, and project configuration.
7. Identify which slice has been requested.
8. Confirm whether its dependencies appear to be complete.
9. Summarise:
   - the active slice;
   - its goal;
   - the work it requires;
   - its dependencies;
   - the relevant files likely to be affected;
   - how it will be verified;
   - and whether completing it should create a Git commit.

Do not modify the project during this inspection.

Do not assume that a task is incomplete solely because its checkbox is unchecked. Compare the documentation against the actual project before implementing it.

---

## Working Scope

Work on only the slice I explicitly authorise.

Within the active slice:

- Complete all tasks required to achieve its documented goal.
- Follow the tasks in a sensible dependency order.
- Treat the goal and intended game behaviour as more important than blindly following the checklist order.
- Do not implement later-slice functionality early.
- Do not expand the feature beyond its documented scope.
- Do not redesign confirmed behaviour without discussing it first.
- Do not introduce unrelated systems, packages, assets, or architectural changes.
- Use temporary test controls or placeholders only when the slice requires behaviour from a later module.
- Clearly label temporary solutions and avoid making later systems depend permanently on them.

If the slice is too large to implement safely as one unit, divide the work into internal task groups while keeping them part of the same active slice.

---

## Clarification Rules

Before implementation, ask for clarification if an ambiguity would materially change:

- player-facing behaviour;
- grid rules;
- coordinate behaviour;
- data ownership;
- public interfaces;
- scene structure;
- saved data;
- dependencies between modules;
- editor workflow;
- or the definition of slice completion.

If the ambiguity is minor and does not affect future compatibility, make the smallest reasonable assumption and report it clearly.

Do not silently invent major game rules or project requirements.

---

## Unity Development Standards

When implementing the slice:

- Keep runtime logic separate from editor-only logic.
- Place editor code in an appropriate `Editor` location or editor-only assembly.
- Keep configuration data separate from mutable runtime state.
- Give each important piece of state one clear owner.
- Avoid direct modification of another module’s internal state.
- Prefer explicit interfaces and queries between systems.
- Avoid global scene searches and hidden dependencies where practical.
- Avoid relying on object names when a stable reference can be used.
- Use clear, consistent naming.
- Keep scripts focused on a defined responsibility.
- Add tooltips, headings, validation, and readable labels to exposed editor fields where useful.
- Prevent invalid configuration where practical.
- Preserve the existing project structure and conventions unless the slice requires a justified change.
- Do not add third-party packages without permission.
- Do not edit generated Unity folders such as `Library`, `Temp`, `Logs`, or `obj`.
- Ensure new Unity assets retain their associated `.meta` files.

The Grid Module should remain usable independently of later gameplay modules wherever the documentation calls for that separation.

---

## Scene and Asset Safety

Before modifying a Unity scene or asset:

- Inspect its current contents and purpose.
- Preserve existing objects and settings unrelated to the active slice.
- Avoid destructive scene restructuring unless explicitly required.
- Do not replace existing user-authored assets without confirmation.
- Do not discard or overwrite unrelated local changes.

If a required Unity scene or asset cannot be safely edited outside the Unity Editor, explain what must be completed manually rather than corrupting or approximating the asset.

---

## Task Tracking

Use the active slice document as the task checklist.

For each task:

1. Confirm what behaviour it is intended to produce.
2. Inspect whether it is already complete.
3. Implement only what is missing.
4. Verify the result.
5. Mark the task complete only after verification succeeds.

When updating Markdown task files:

- Change completed checkboxes from `[ ]` to `[x]`.
- Do not rewrite the documented goal or requirements unless asked.
- Do not mark partially completed work as complete.
- Add a short implementation note only when it provides useful future context.
- Record blockers honestly.
- Preserve the existing structure of the document.

The documentation and the implementation must remain consistent.

---

## Verification Requirements

A slice is not complete merely because the code compiles.

Use every verification method available and appropriate, including:

- Unity Edit Mode tests;
- Unity Play Mode tests;
- targeted unit tests;
- project compilation;
- scene validation;
- editor validation;
- debug visualisations;
- runtime state inspection;
- and manual verification instructions.

Verify at minimum:

- the normal case;
- an invalid case;
- a boundary case;
- and an integration case where applicable.

Where Unity cannot be launched or a test cannot be executed in the current environment:

- perform all safe static verification available;
- do not claim that unexecuted tests passed;
- provide exact manual Unity verification steps;
- clearly distinguish verified behaviour from behaviour awaiting Unity confirmation.

Do not hide warnings or test failures. Determine whether they were introduced by the active work and report them accurately.

---

## Definition of Done

A slice is complete only when:

- its documented goal has been achieved;
- all required tasks are complete;
- acceptance criteria have been checked;
- the implementation compiles, where compilation is available;
- appropriate tests or verification tools exist;
- relevant tests pass, where they can be run;
- editor and debug support required by the slice is available;
- documentation reflects the actual implementation;
- temporary limitations are documented;
- no known regression caused by the slice remains unresolved;
- and the project is in a coherent state for the next slice.

Do not begin the next slice automatically.

---

## Git Workflow

Use Git to preserve completed major slices.

### Before Work

- Inspect `git status`.
- Identify existing modified or untracked files.
- Treat pre-existing changes as user-owned.
- Do not discard, overwrite, reset, clean, or include unrelated changes.
- Determine whether the repository is currently in a safe state for the active work.

### Commit Classification

A **major slice** normally introduces a complete new capability or foundation, such as:

- a new runtime system;
- a new authoring workflow;
- a meaningful public interface;
- a complete playable or testable behaviour;
- a major integration point;
- or a milestone that later slices depend upon.

A **minor slice** normally contains:

- a small extension;
- focused validation;
- debugging support;
- editor polish;
- isolated corrections;
- or supporting work that does not create a significant standalone milestone.

If the classification is unclear, state your recommendation before committing.

### Commit Rules

For a major slice:

1. Complete and verify the entire slice.
2. Review the final diff.
3. Confirm that unrelated changes are excluded.
4. Create one focused commit for the completed slice.
5. Use a clear commit message describing the delivered capability.

Suggested format:

```text
feat(grid): complete slice NN short description

For fixes:

fix(grid): describe corrected behaviour

For editor tooling:

tools(grid): describe editor capability

For tests only:

test(grid): describe verified behaviour

For a minor slice:

Complete and verify the work.
Leave it uncommitted unless I explicitly request a commit.
Report that it is ready to be included in a later suitable commit.

Do not create a commit when:

tests required for completion are failing;
the slice is incomplete;
unresolved conflicts remain;
unrelated changes cannot be separated safely;
or I have asked you not to commit.

Never amend, squash, rebase, reset, force-push, or push to a remote unless I explicitly request it.

Completion Report

When the active slice is finished, report:

Outcome
What capability now exists.
Completed Tasks
Which documented tasks were completed.
Implementation Summary
The important systems, files, assets, or editor tools added or changed.
Verification
What was tested, how it was tested, and the result.
Manual Unity Checks
Any checks that still need to be performed inside Unity.
Assumptions and Limitations
Any assumptions, placeholders, deferred behaviour, or known limitations.
Git Status
Whether the work was committed, intentionally left uncommitted, or blocked from commit.
Commit Details
If committed, provide the commit hash and message.
Next Available Slice
Identify the likely next slice, but do not start it without authorisation.