# Contributing to BG

This repository uses behavior-first workflow design rules.

Before implementing UI/UX changes, all contributors must read:

- WORKFLOW_REFORM_INSTRUCTIONS.md
- docs/instructions/ARCHITECTURE.md
- docs/instructions/DAILY_OPERATIONS_MATRIX.md
- docs/instructions/LOCAL_DEVELOPMENT.md

## Mandatory Rule

Do not submit UI changes that only improve appearance while leaving behavior ambiguity unchanged.

UI work must reduce cognitive load by structural constraints (state, ordering, eligibility, control availability), not by additional explanatory text.

## Required PR Evidence

Any PR touching the listed workflow surfaces must include:

1. Core lever implemented from WORKFLOW_REFORM_INSTRUCTIONS.md (choose one primary lever):
	- Visual Sequencing
	- Reduce Parallelism
	- Reduce Decision Density
	- Decision Weighting in Queues
	- Silent Alternative Path
	- Contextual Entry
2. Which cognitive-load source was removed.
3. Which structural constraint was added.
4. Which cross-cutting principle(s) were applied, if any:
	- Show Governance Early
	- Safe-by-Default
	- Minimize Textual Guidance
5. Which text guidance was removed or reduced.
6. Which surface checklist items were satisfied.
7. Expected measurable operational impact.

## Review Gate

Reviewers should reject PRs that:

- Add guidance banners/alerts without adding structural constraints.
- Keep multiple same-weight primary actions in one active state.
- Preserve invalid interaction paths and rely on late error messaging.

## Automated PR Gate

This repository enforces PR workflow reform compliance in CI using:

- .github/workflows/pr-workflow-reform-gate.yml
- scripts/ci/validate_pr_workflow_reform.py

Non-draft PRs will fail if they do not:

- Select exactly one primary core lever checkbox
- Fill cognitive-load source removed
- Fill structural constraint added
- Fill expected measurable impact

## Validation Expectations

When relevant, include:

- Updated tests for behavior changes.
- Build and test command outputs.
- Before/after screenshots or short clips focused on task flow.
