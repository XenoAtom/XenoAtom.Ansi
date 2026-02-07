# XenoAtom.Ansi — Codex Agent Instructions

Fast, allocation-friendly .NET library for generating/parsing ANSI/VT escape sequences (`net10.0`, AOT-friendly).

Paths/commands below are relative to this directory.

## Orientation

- Library: `src/XenoAtom.Ansi/`
- Tests: `src/XenoAtom.Ansi.Tests/` (MSTest)
- Benchmarks: `src/XenoAtom.Ansi.Benchmarks/`
- Samples: `samples/`
- Docs to keep in sync with behavior: `readme.md` and `doc/**/*.md` (esp. `doc/readme.md`)

## Build & Test

```sh
cd src
dotnet build -c Release
dotnet test -c Release
```

All tests must pass; update docs/readme for any behavior or public API changes.

## Contribution Rules (Do/Don't)

- Keep diffs focused; avoid drive-by refactors/formatting and unnecessary dependencies.
- Bug fix = regression test first (failing), then fix.
- New/changed behavior requires tests and docs updates.
- Keep hot paths allocation-friendly (`Span<T>`, `ArrayPool<T>`, avoid avoidable string allocations/exceptions).
- Don't hand-edit generated files (e.g., `*.g.cs`) unless that is the intended workflow.

## C# / API Conventions

- Nullability is enabled: respect annotations; validate inputs early (`ArgumentNullException.ThrowIfNull()` and friends); don't suppress warnings without a justification comment.
- Public APIs require XML docs and should document thrown exceptions.
- Keep code trimmer/AOT friendly: avoid reflection; prefer source generators; annotate if reflection is unavoidable.
- Prefer overloads over optional parameters (binary compatibility); consider `Try*` APIs for non-throwing hot paths.
- Async: `Async` suffix; no `async void` (except event handlers); use `ConfigureAwait(false)` in library code.

## Git / Pre-submit

- Before committing: build+tests pass; docs updated if behavior changed.
- Commits: imperative subject, < 72 chars; one logical change per commit; reference issues when relevant.
