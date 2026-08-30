Create a reusable Notion project template for documenting the design, development and testing of a video game.

The first project using this template is titled **Grid-Based Strategy Game**, but the structure should be reusable for future game projects.

Use **Grid-Based Strategy Game** as the current display name and `grid-based-strategy-game` when a technical identifier, repository name or folder name is required.

This is the first structural pass. Create the page hierarchy, reusable formats and placeholders only.

Do not write a complete game specification, invent mechanics or create every anticipated Module and Slice.

# Required page hierarchy

Create the following hierarchy:

**Grid-Based Strategy Game — Development Project**

* **Game Design Brief**
* **Module 01 — Example Module**

  * **Slice 01 — Example Slice**
  * **Slice 02 — Example Slice**
* **Module 02 — Example Module**

  * **Slice 01 — Example Slice**
  * **Slice 02 — Example Slice**
* **Project Decision Log**
* **Development Log**

Create only:

* One Game Design Brief page.
* Two example Module pages.
* Two example Slice pages inside each Module.
* One Project Decision Log.
* One Development Log.

Do not create all anticipated Modules or Slices. The example pages exist to establish formats that can later be duplicated, renamed and edited.

# Main project page

Create a parent page titled:

**Grid-Based Strategy Game — Development Project**

Give it the following sections.

## Project Snapshot

Create a compact project overview containing:

* Project name.
* Project identifier.
* Genre.
* Engine.
* Target platform.
* Current development phase.
* Current Module.
* Current Slice.
* Prototype status.
* Repository name.
* Last updated date.

Use placeholders where information has not been confirmed.

## Project Navigation

Create clear links to:

* Game Design Brief.
* Module 01.
* Module 02.
* Project Decision Log.
* Development Log.

## Current Focus

Create a compact working section containing:

* Current objective.
* Current Module.
* Current Slice.
* Next deliverable.
* Current blocker.
* Next action.

Use placeholders instead of inventing project status.

## Development Method

Add a short explanation of the intended workflow:

1. Define the overall game in the Game Design Brief.
2. Divide the game into independent Modules.
3. Divide each Module into smaller Slices.
4. Specify one Slice before implementing it.
5. Build the Slice.
6. Test the Slice against its acceptance criteria.
7. Record decisions and implementation results.
8. Update the parent Module.
9. Continue to the next Slice.

Explain that:

* A **Module** represents a major game system.
* A **Slice** represents a smaller deliverable within that system.
* Each Slice should be independently understandable, implementable and testable.
* Completing every required Slice should result in the completed Module.

## Module Directory

Create a simple directory containing only the two example Modules.

Show:

* Module number.
* Module name.
* Purpose.
* Current Slice.
* Specification status.
* Implementation status.
* Testing status.
* Overall status.

Set both example Modules to:

**Template example**

Do not invent final Module names or responsibilities.

## Project Milestones

Create placeholder milestone categories for:

* Pre-production.
* Project setup.
* Core prototype.
* First playable match.
* Mechanics-first vertical slice.
* Content production.
* Presentation and polish.
* Release preparation.

Do not create detailed milestones or mark anything complete.

# Game Design Brief page

Create a child page titled:

**Game Design Brief**

This page will hold the authoritative high-level description of the game.

Detailed technical specifications should remain inside the relevant Modules and Slices.

Give the Game Design Brief the following structure.

## Document Status

Include:

* Version.
* Status.
* Last updated.
* Author.
* Current review stage.

## High Concept

Add a placeholder for a concise explanation of the game.

The completed High Concept should eventually explain:

* What the game is.
* What the player does.
* What makes the game distinctive.

## Player Fantasy

Add a placeholder explaining:

* The role the player inhabits.
* The experience the game should create.
* The decisions the player should make.
* How the player should feel while playing.

## Genre and Format

Add placeholders for:

* Genre.
* Subgenre.
* Perspective.
* Visual format.
* Intended platforms.
* Player count.
* Match or session length.
* Game modes.
* Input method.

## Target Audience

Add placeholders for:

* Intended player.
* Expected experience level.
* Comparable games.
* Accessibility considerations.
* Desired difficulty.

## Design Pillars

Create space for three to five Design Pillars.

Each pillar should contain:

* Pillar name.
* Meaning.
* How the game supports it.
* Features that would contradict it.

## Core Game Loop

Create an empty numbered sequence for the main player loop.

## Match or Session Flow

Create placeholders for:

* Before the match or session.
* Setup.
* Beginning of play.
* Main gameplay.
* Escalation.
* Victory or failure.
* Post-match progression.

## Core Mechanics

Create a section for short summaries of the game’s major mechanics.

Do not create complete specifications here.

For each mechanic, provide space for:

* Mechanic name.
* Player purpose.
* High-level behaviour.
* Related Module.
* Current status.

## Progression and Unlocks

Add placeholders for:

* Player progression.
* Content unlocks.
* Roster or character progression.
* Difficulty progression.
* Rewards.
* Long-term goals.

## Content Structure

Add placeholders for:

* Playable factions, races or characters.
* Units.
* Classes.
* Arenas or levels.
* Game modes.
* Abilities.
* Objectives.
* Items or equipment, if applicable.

## Victory and Failure

Add placeholders for:

* Primary victory condition.
* Alternative victory conditions.
* Failure conditions.
* Draw or stalemate handling.
* Surrender or match abandonment.

## Prototype Scope

Separate this into two sections.

### Included in the first prototype

Add placeholders for the minimum systems required to prove the game.

### Excluded from the first prototype

Add placeholders for features intentionally deferred.

## Presentation Direction

Add placeholders for:

* Visual style.
* Audio direction.
* Interface style.
* Camera.
* Animation requirements.
* Placeholder presentation.
* Feedback and readability.

## Technical Direction

Add high-level placeholders for:

* Engine.
* Target hardware.
* Offline or online requirements.
* Save requirements.
* Data-driven systems.
* Modularity requirements.
* Performance targets.

Keep this section high level. Detailed implementation decisions belong inside Modules and Slices.

## Open Design Questions

Create an empty list for unresolved high-level decisions.

## Confirmed Decisions

Create an empty dated list containing:

* Date.
* Decision.
* Reason.
* Affected Modules.
* Status.

## Deferred Ideas

Create an area for ideas that may be valuable later but are outside the current scope.

# Module page template

Create two example Module pages using identical formatting.

Title them:

* **Module 01 — Example Module**
* **Module 02 — Example Module**

A Module represents a major game system with a clear responsibility.

Each Module page should contain the following sections.

## Module Status

Include:

* Module number.
* Module name.
* Status.
* Current Slice.
* Specification status.
* Implementation status.
* Testing status.
* Overall completion.
* Last updated.

## Module Purpose

Add a placeholder explaining:

* Why the Module exists.
* What problem it solves.
* What player-facing or technical outcome it provides.

## Responsibilities

Add a placeholder for the rules, data, systems and behaviours owned by the Module.

## Exclusions

Add a placeholder for responsibilities that explicitly belong to other Modules.

## Player-Facing Behaviour

Add a placeholder explaining what the player sees, controls or experiences when the Module works correctly.

## Module Boundaries

Add placeholders defining:

* What information enters the Module.
* What information leaves the Module.
* What the Module may change.
* What the Module must never change directly.

## Dependencies

Separate this into two sections.

### Depends on

List the Modules, services or data this Module requires.

### Used by

List the Modules or systems that consume this Module.

## Data and Runtime State

Add placeholders for:

* Configuration data.
* Permanent definitions.
* Runtime state.
* Saved data.
* Events.
* Commands.
* Queries.
* Results.
* Failure states.

## Rules and Invariants

Add placeholders for rules that must always remain true while the Module is operating.

## Locked Decisions

Create an empty dated list for confirmed Module decisions.

Each entry should include:

* Date.
* Decision.
* Reason.
* Affected Slices.
* Status.

## Open Questions

Create an empty list for unresolved Module decisions.

## Slice Directory

Show only the two example Slice pages contained inside the Module:

* Slice 01 — Example Slice.
* Slice 02 — Example Slice.

Give each entry the following fields:

* Slice number.
* Slice name.
* Purpose.
* Dependencies.
* Specification status.
* Implementation status.
* Testing status.
* Overall status.

## Module Acceptance Criteria

Add an empty numbered section defining what must be true before the complete Module can be considered finished.

Acceptance criteria should eventually be:

* Observable.
* Testable.
* Unambiguous.
* Independent of presentation where possible.

## Module Testing Strategy

Add placeholders for:

* Unit testing.
* Integration testing.
* Manual Unity testing.
* Regression testing.
* Performance testing, where relevant.

## Deferred Features

Add a placeholder for features that belong to the Module but will not be included in its first implementation.

## Known Limitations

Add an empty section for accepted limitations in the current version.

## Module Development Log

Add a dated log containing:

* Date.
* Slice.
* Work completed.
* Changes made.
* Problems encountered.
* Decisions made.
* Tests performed.
* Result.
* Follow-up work.

# Slice page template

Inside each example Module, create exactly two example Slice pages:

* **Slice 01 — Example Slice**
* **Slice 02 — Example Slice**

All four Slice pages must use identical formatting.

A Slice is a small, independently understandable part of a Module. It should be possible to specify, implement and test the Slice without completing the entire Module.

Each Slice page should contain the following sections.

## Slice Status

Include:

* Parent Module.
* Slice number.
* Slice name.
* Status.
* Specification status.
* Implementation status.
* Testing status.
* Overall completion.
* Last updated.

## Slice Goal

Add a concise placeholder explaining the specific result this Slice should achieve.

## Reason for the Slice

Add a placeholder explaining:

* Why this work is needed.
* What later work depends on it.
* Why it is separate from the other Slices.

## Player-Visible Outcome

Add a placeholder describing what the player can see, control or experience when the Slice is complete.

If the Slice has no direct player-facing output, explain what technical capability becomes available.

## Included

Add an empty list for behaviours and requirements included in this Slice.

## Not Included

Add an empty list for related behaviours intentionally excluded from this Slice.

## Dependencies

Add placeholders for:

* Required earlier Slices.
* Required Modules.
* Required Unity systems.
* Required assets.
* Required data.
* Blocking decisions.

## Rules and Behaviour

Add placeholders for the exact rules the Slice must implement.

Rules should eventually be written clearly enough that two developers would implement the same behaviour.

## Data Requirements

Add placeholders for:

* Inputs.
* Outputs.
* Configuration.
* Permanent definitions.
* Runtime state.
* Events.
* Commands.
* Queries.
* Results.
* Failure results.

## Interaction Flow

Create an empty numbered flow showing how the Slice operates from input to final result.

## Implementation Plan

Create an empty numbered sequence for the intended implementation order.

Do not invent code or architecture before the Slice is properly specified.

## Edge Cases

Add an empty list for:

* Invalid input.
* Missing data.
* Boundary behaviour.
* Conflicting states.
* Interrupted operations.
* Repeated commands.
* Failure recovery.

## Acceptance Criteria

Create an empty numbered list of observable and testable completion conditions.

Every acceptance criterion should eventually describe one unambiguous result.

## Testing Plan

Separate this into the following sections.

### Unit tests

Add placeholders for isolated rule and data tests.

### Integration tests

Add placeholders for interactions with other Modules and Slices.

### Manual Unity tests

Add placeholders for behaviours that must be confirmed inside Unity.

### Regression tests

Add placeholders for existing behaviour that must continue working.

## Debugging Requirements

Add placeholders for:

* Logs.
* Inspector information.
* Debug overlays.
* Validation errors.
* Development-only controls.

## Implementation Log

Create a dated log containing:

* Date.
* Work completed.
* Problems encountered.
* Decisions made.
* Tests performed.
* Result.
* Known limitations.
* Next action.

## Completion Summary

Add placeholders for:

* Final result.
* Acceptance criteria passed.
* Tests passed.
* Known limitations.
* Deferred work.
* Required documentation updates.
* Recommended next Slice.

# Project Decision Log

Create a page titled:

**Project Decision Log**

Use this page for decisions that affect the Game Design Brief or more than one Module.

Each entry should contain:

* Date.
* Decision.
* Reason.
* Affected Modules.
* Affected Slices.
* Alternatives considered.
* Consequences.
* Status.
* Follow-up action.

Do not add invented decisions.

# Development Log

Create a page titled:

**Development Log**

Use it to track development progress across the entire project.

Each entry should contain:

* Date.
* Development phase.
* Module.
* Slice.
* Work completed.
* Problems encountered.
* Decisions made.
* Tests performed.
* Result.
* Known limitations.
* Current blocker.
* Next action.

Do not include video production or recording information.

# Template rules

* Create a clean and reusable game-development structure.
* Keep the template focused on game design, implementation and testing.
* Do not include YouTube planning, recording status, footage, retakes or production information.
* Do not reference or import an existing Notion workspace.
* Do not invent game mechanics, statistics, units, classes or implementation details.
* Use placeholders wherever a decision has not been made.
* Create only one Game Design Brief page.
* Create only two example Module pages.
* Create exactly two example Slice pages inside each example Module.
* Use identical formatting across both Module pages.
* Use identical formatting across all four Slice pages.
* Make every example Module and Slice suitable for duplication.
* Use headings, dividers and compact tables where they improve navigation.
* Avoid unnecessary decoration and large databases.
* Do not create additional Modules or Slices during this first pass.
* Keep the hierarchy easy to understand.
* Finish by reporting the complete page hierarchy.
* Clearly identify which pages should be duplicated when creating future Modules and Slices.
