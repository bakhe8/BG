# BG Testing Instructions

Apply these rules when creating or editing tests in this repository.

## Scope

- The primary test project is `tests/BG.UnitTests`
- Use `xUnit`
- Prefer unit tests first; add integration tests only when the behavior crosses process, database, or HTTP boundaries

## Test priorities

Write tests in this order:

1. `Application` services and orchestration logic
2. `Domain` behavior and invariants
3. `Web` controller/page-model behavior that contains logic worth protecting
4. `Infrastructure` only when configuration or mapping behavior is non-trivial

## Style

- Use clear `Arrange / Act / Assert` structure
- Keep one behavior per test
- Name tests by expected behavior, not by method name alone
- Prefer simple stubs/fakes over heavy mocking
- Do not hit PostgreSQL, IIS, or real hospital APIs in unit tests
- Avoid snapshot tests for API payloads unless the payload is stable and intentionally versioned

## Assertions

- Assert the behavior that matters to the contract
- For API/controller tests, prefer typed response models over anonymous objects
- For time-based values, assert ranges or invariants rather than exact timestamps
- For localization and theming behavior, test normalization, defaults, cookie persistence, and direction changes without requiring a browser

## AI workflow

- When asked to add tests, extend existing test files before creating new ones unless a new area is clearly justified
- If production code is hard to test because it returns anonymous objects or hides behavior in static calls, refactor toward typed and injectable code first
- Keep test data minimal and readable
- When a bug is fixed, add or update a regression test in the same change

## Commands

- Build: `dotnet build BG.sln`
- Run tests: `dotnet test BG.sln`
- Run tests with coverage: `dotnet test BG.sln --collect:\"XPlat Code Coverage\"`
