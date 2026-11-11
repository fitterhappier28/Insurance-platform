# Issue tracker: Local Markdown in specs

Tickets for this repo live as markdown files committed alongside the specs they implement.

## Conventions

- The implementation plan is `specs/implementation/plan.md`.
- One file per ticket at `specs/implementation/tickets/<NN>-<slug>.md`, numbered from `01` in dependency order. Never a single combined tickets file.
- Each ticket has `**Blocked by:**` and `**Status:**` lines near the top. Status strings are in `triage-labels.md`, plus `in-progress` and `done`.
- Acceptance criteria cite requirement ids (`FR-n`, `NFR-n`) from `specs/product/`.
- Comments and conversation history append to the bottom of the file under a `## Comments` heading.

## When a skill says "publish to the issue tracker"

Create a new file under `specs/implementation/tickets/` with the next free number. A new feature-sized effort gets its own spec under `specs/product/` first.

## When a skill says "fetch the relevant ticket"

Read the file at the referenced path. The user will normally pass the path or the ticket number directly.

## Frontier

A ticket is ready when its status is `ready-for-agent` or `ready-for-human` and every ticket listed in `Blocked by` is `done`. Lowest number wins.
