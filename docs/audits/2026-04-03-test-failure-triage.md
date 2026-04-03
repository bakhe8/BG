# Test Failure Triage - 2026-04-03

Command baseline:

- dotnet test BG.sln -c Debug --no-build

Result snapshot:

- Failed: 47
- Passed: 190
- Skipped: 0
- Total: 237

## Failure Distribution

- Web: 21
- Hosted: 9
- Integrations: 11
- Application: 4
- Domain: 2

## Root-Cause Clusters

1. Behavioral contract shift from PageResult to RedirectToPageResult (high impact)

- Symptoms:
  - Assert.IsType failure where tests expected PageResult but handlers now redirect silently on blocked/stale/ineligible cases.
- Affected areas:
  - Approvals queue handler tests.
  - Operations queue handler tests.
- Recommended fix:
  - Update assertions to validate redirect route/parameters and selection fallback semantics.

1. New queue pre-selection call path breaks older test stubs (high impact)

- Symptoms:
  - NotSupportedException from StubApprovalQueueService.GetWorkspaceAsync.
- Affected areas:
  - Approval dossier page tests.
- Recommended fix:
  - Extend dossier page test doubles to implement queue workspace lookup used by ResolvePreferredQueueRequestIdAsync.

1. Index routing behavioral change causing NRE in unit tests and redirect mismatch in hosted tests (high impact)

- Symptoms:
  - NullReferenceException in IndexModel.OnGetAsync line 32 for unit tests.
  - Hosted tests expecting 200 OK now receive redirect (Found).
- Affected areas:
  - IndexPageTests.
  - Hosted authentication/home flow tests.
- Recommended fix:
  - Add test setup for Request/Query context used by stay=1 logic and workspace shell assumptions.
  - Update hosted assertions to accept intended redirect policy for authenticated users.

1. Actor/session expectations in web tests now inconsistent with handler fallback model (medium-high impact)

- Symptoms:
  - NullReferenceException or nullable value errors in approval/operations/dispatch/requests web page tests.
  - Expected actor ids now null because fallback/selection path differs.
- Recommended fix:
  - Refit test fixtures with active actor + selectable items matching new guard conditions.
  - Assert outcome semantics (redirect target and selected item) instead of previous in-page message assumptions.

1. Domain/Application dispatch lifecycle state mismatch (medium impact)

- Symptoms:
  - Expected AwaitingBankResponse, actual SubmittedToBank.
  - Reopen dispatch invalid-operation guard triggered.
- Affected areas:
  - Guarantee aggregate tests.
  - Dispatch workspace service tests.
  - Operations review queue service status expectation.
- Recommended fix:
  - Reconcile expected post-dispatch state with updated aggregate rules and update affected assertions.

1. OCR integration harness missing local environment (environmental blocker)

- Symptoms:
  - InvalidOperationException requiring .venv-ocr312/Scripts/python.exe.
- Affected areas:
  - LocalPythonOcrProcessingServiceTests (11 failures).
- Recommended fix:
  - Provision local OCR env before running mandatory OCR suite, or conditionally skip locally with explicit guard policy.

## Proposed Remediation Order

1. Web + Hosted contract updates for redirect-first behavior and actor/selection fixtures.
2. Dossier test doubles upgrade for queue pre-selection service call.
3. Domain/Application expected state alignment for dispatch lifecycle.
4. OCR environment provisioning and rerun integration suite.

## Immediate Next Commands

- dotnet test tests/BG.UnitTests/BG.UnitTests.csproj -c Debug --no-build --filter "FullyQualifiedName~BG.UnitTests.Web|FullyQualifiedName~BG.UnitTests.Hosted"
- dotnet test tests/BG.UnitTests/BG.UnitTests.csproj -c Debug --no-build --filter "FullyQualifiedName~BG.UnitTests.Domain|FullyQualifiedName~BG.UnitTests.Application"
- dotnet test tests/BG.UnitTests/BG.UnitTests.csproj -c Debug --no-build --filter "FullyQualifiedName~BG.UnitTests.Integrations.LocalPythonOcrProcessingServiceTests"

## Remediation Execution Update (2026-04-03)

- Web remediation: Completed (redirect-first handler contract and fixture alignment).
- Hosted remediation: Completed.
  - Command: dotnet test tests/BG.UnitTests/BG.UnitTests.csproj -c Debug --filter "FullyQualifiedName~BG.UnitTests.Hosted" -v minimal
  - Result: Failed 0, Passed 19, Skipped 0, Total 19
- Domain/Application dispatch lifecycle alignment: Completed.
  - Command: dotnet test tests/BG.UnitTests/BG.UnitTests.csproj -c Debug --filter "FullyQualifiedName!~BG.UnitTests.Integrations.LocalPythonOcrProcessingServiceTests" -v minimal
  - Result: Failed 0, Passed 226, Skipped 0, Total 226

## Residual Deferred Stream

- OCR integration suite is deferred by explicit operator decision for a later dedicated pass:
  - LocalPythonOcrProcessingServiceTests continue to require local OCR runtime at .venv-ocr312/Scripts/python.exe and src/BG.Integrations/OcrWorker/ocr_worker.py.
  - No remaining non-OCR test failures after remediation.
