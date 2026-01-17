# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VsaResults is a C# library providing a fluent discriminated union (`ErrorOr<T>`) for Vertical Slice Architecture. It's a modernized fork of [ErrorOr](https://github.com/amantinband/error-or) with additional features including .NET 10 support, feature pipelines, wide events observability, and ASP.NET Core integration.

**Package**: `dotnet add package VsaResults`

## Commands

### Setup

```bash
dotnet restore
```

### Development

```bash
dotnet watch run --project src/ErrorOr
```

### Build

| Context | Command |
|---------|---------|
| All (Release, warnings as errors) | `dotnet build -c Release -warnaserror` |
| Debug | `dotnet build` |

### Test

| Context | Framework | Command |
|---------|-----------|---------|
| All tests | xUnit | `dotnet test -c Release` |
| Specific class | xUnit | `dotnet test --filter "FullyQualifiedName~ThenTests"` |
| Single test | xUnit | `dotnet test --filter "FullyQualifiedName~CallingThen_WhenIsSuccess_ShouldInvokeGivenFunc"` |

### Quality

```bash
# Build with analyzers (lint)
dotnet build -c Release -warnaserror

# Pack NuGet package
dotnet pack -c Release -o ./artifacts
```

## Architecture

### Directory Structure

```
src/
├── ErrorOr/
│   ├── ErrorOr.cs              # Main discriminated union type
│   ├── ErrorOr.*.cs            # Partial classes by feature (Then, Match, Switch, etc.)
│   ├── Features/               # VSA feature pipeline system
│   ├── WideEvents/             # Observability via wide events
│   ├── AspNetCore/             # ASP.NET Core integration
│   └── Serialization/          # JSON serialization support
├── Errors/
│   └── Error.cs                # Error representation
tests/
└── ErrorOr/                    # xUnit tests with FluentAssertions
```

### Key Patterns

1. **Discriminated Union**: `ErrorOr<TValue>` is a readonly record struct wrapping either a value or error list
2. **Fluent Chaining**: Methods return `ErrorOr<T>` enabling `result.Then().FailIf().Else().Match()`
3. **Async Variants**: Most methods have async versions (`ThenAsync`, `MatchAsync`, `ElseAsync`)
4. **Context Propagation**: Wide event context flows through transformations via `WithContext()`
5. **Feature Pipeline**: VSA pattern with `IQueryFeature` and `IMutationFeature` interfaces

### Key Files

| File | Purpose |
|------|---------|
| `src/ErrorOr/ErrorOr.cs` | Main discriminated union type |
| `src/Errors/Error.cs` | Error representation with factory methods |
| `src/ErrorOr/Features/` | VSA feature pipeline interfaces |
| `src/ErrorOr/WideEvents/` | Wide events observability system |
| `src/ErrorOr/AspNetCore/` | Minimal API and MVC integration |

## Git Workflow

When asked to commit changes:

1. **Review changes**: Run `git status` and `git diff`
2. **Check style**: Run `git log --oneline -5` to match existing commit message style
3. **Stage files**: `git add` relevant files
4. **Commit** using [Conventional Commits](https://www.conventionalcommits.org/):
   - `feat:` new features
   - `fix:` bug fixes
   - `docs:` documentation changes
   - `refactor:` code refactoring
   - `test:` test changes
   - `chore:` maintenance tasks
5. **Include**: `Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>`

### Branch Naming

- Feature branches: `feat/<ticket-id>-<short-description>`
- Bug fixes: `fix/<ticket-id>-<short-description>`

Do NOT push to remote unless explicitly asked.

## Conventions

1. Always read before editing - Use the Read tool before making changes
2. Test coverage required - New code must include xUnit tests with FluentAssertions
3. Use Arrange-Act-Assert pattern with explicit comments in tests
4. Follow existing partial class organization by feature
5. Maintain readonly record struct semantics for core types
6. Include async variants for new chainable methods

## Test Conventions

- Framework: xUnit with FluentAssertions
- Location: `tests/` directory
- Pattern: `{FeatureName}Tests.cs` with `[Fact]` attributes
- Structure: Arrange-Act-Assert with explicit comments
- Test utilities in `tests/ErrorOr/TestUtils.cs`
