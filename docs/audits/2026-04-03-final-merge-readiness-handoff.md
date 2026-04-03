# Final Merge-Readiness Handoff

Date: 2026-04-03
Owner: Workflow reform execution stream

## Purpose

This handoff package gives reviewers a deterministic walkthrough for validating behavioral reform outcomes before merge.

## Reviewer Walkthrough

1. Approvals surface

- Open Approvals queue and select a blocked item.
- Confirm the primary path is dossier access, not decision execution.
- Confirm Return and Reject are secondary actions for actionable items.

1. Dispatch surface

- Open a ready item where actor can dispatch and print.
- Confirm Dispatch is primary and Print is under More actions.
- Open pending-delivery item and confirm Confirm Delivery is primary, Reopen is secondary.

1. Operations surface

- Confirm Apply is primary in active panel.
- Confirm Return and Reject are secondary under More actions.
- Attempt stale item action and confirm silent redirect to valid selection.

1. Requests surface

- Validate primary action follows state capability (Submit, Update, Withdraw, Cancel).
- Confirm destructive alternatives are grouped under More actions.
- Confirm stale or ineligible postbacks redirect to a valid context without late blocking errors.

## CI Gate Confirmation Checklist

- Workflow reform PR template gate present and passing:
  - .github/workflows/pr-workflow-reform-gate.yml
- PR template includes path/goal alignment and role-scenario evidence link.
- Completion log and execution compass remain aligned with this batch.

## Local Validation Snapshot

- dotnet build BG.sln -c Debug: Passed
- dotnet test tests/BG.UnitTests/BG.UnitTests.csproj -c Debug --filter "FullyQualifiedName~BG.UnitTests.Hosted" -v minimal: Passed (19/19)
- dotnet test tests/BG.UnitTests/BG.UnitTests.csproj -c Debug --filter "FullyQualifiedName!~BG.UnitTests.Integrations.LocalPythonOcrProcessingServiceTests" -v minimal: Passed (226/226)
- OCR suite status: Deferred by explicit operator decision for a later dedicated OCR stabilization wave

## Handoff Status

- Reviewer walkthrough package: Ready
- CI gate confirmation: Pending PR run
- Test posture for this handoff: Non-OCR suites are green and accepted; OCR stream deferred by decision
