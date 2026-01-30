# AGENTS.md

This file provides guidance to Codex CLI when working with code in this repository.

## Project Overview

VsaResults is a C# library providing a fluent discriminated union (`ErrorOr<T>`) for Vertical Slice Architecture. It's a modernized fork of [ErrorOr](https://github.com/amantinband/error-or) with additional features including .NET 10 support, feature pipelines, wide events observability, and ASP.NET Core integration.

**Package**: `dotnet add package VsaResults`

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build (Release with warnings as errors - same as CI)
dotnet build -c Release -warnaserror

# Run all tests
dotnet test -c Release

# Run a specific test class
dotnet test --filter "FullyQualifiedName~ThenTests"

# Run a single test
dotnet test --filter "FullyQualifiedName~CallingThen_WhenIsSuccess_ShouldInvokeGivenFunc"

# Pack NuGet package
dotnet pack -c Release -o ./artifacts
```

## Architecture

### Core Types (`src/`)

**`ErrorOr<TValue>`** - The main discriminated union type (`src/ErrorOr/ErrorOr.cs`)
- Readonly record struct wrapping either a value or a list of errors
- Supports context propagation via `ImmutableDictionary<string, object>`
- Partial class split across multiple files by feature (Then, Match, Switch, Else, FailIf, etc.)

**`Error`** - Error representation (`src/Errors/Error.cs`)
- Readonly record struct with Code, Description, Type, and optional Metadata
- Factory methods for built-in types: `Error.Validation()`, `Error.NotFound()`, `Error.Unauthorized()`, etc.
- Custom errors via `Error.Custom(type, code, description)`

### Feature Pipeline System (`src/ErrorOr/Features/`)

The library implements a VSA feature pipeline with these interfaces:

- **`IQueryFeature<TRequest, TResult>`** - Read-only operations: Validate → Execute Query
- **`IMutationFeature<TRequest, TResult>`** - State-changing operations: Validate → Enforce Requirements → Execute Mutation → Run Side Effects

Pipeline components:
- `IFeatureValidator<TRequest>` - Request validation
- `IFeatureRequirements<TRequest>` - Load entities, enforce business rules
- `IFeatureMutator<TRequest, TResult>` - Core mutation logic
- `IFeatureQuery<TRequest, TResult>` - Query execution
- `IFeatureSideEffects<TRequest>` - Post-success effects (notifications, etc.)

Each component has a no-op default (`NoOpValidator`, `NoOpRequirements`, `NoOpSideEffects`).

### Wide Events / Canonical Log Lines (`src/ErrorOr/WideEvents/`)

One structured log event per feature execution containing full context:
- `WideEvent` - The event model with composable segments (Feature, Error, Message, Context)
- `WideEventBuilder` - Fluent builder accumulated during execution
- `IWideEventEmitter` - Abstraction for emitting events (`EmitAsync`/`Emit`)

### ASP.NET Core Integration (`src/ErrorOr/AspNetCore/`)

- `FeatureHandler` - Static factory for Minimal API delegates
- `FeatureController` - Base controller with `ToActionResult()` methods
- `ApiResults` - IResult factory mapping ErrorOr to HTTP responses
- `ActionResultExtensions` - Extension methods for MVC controllers

### JSON Serialization (`src/ErrorOr/Serialization/`)

- `ErrorOrJsonConverterFactory` - Auto-registers converters with System.Text.Json
- `ErrorOrJsonConverter<T>` - Serializes as `{ isError, value?, errors? }`
- `ErrorJsonConverter` - Handles Error serialization with metadata

## Test Conventions

- Framework: xUnit with FluentAssertions
- Location: `tests/` directory
- Pattern: `{FeatureName}Tests.cs` with `[Fact]` attributes
- Arrange-Act-Assert structure with explicit comments
- Test utilities in `tests/ErrorOr/TestUtils.cs`

## Key Patterns

**Fluent chaining**: Methods return `ErrorOr<T>` enabling:
```csharp
result.Then(transform).FailIf(predicate, error).Else(fallback).Match(onValue, onError)
```

**Async variants**: Most methods have async versions (`ThenAsync`, `MatchAsync`, `ElseAsync`)

**Extension methods**: `Task<ErrorOr<T>>` extensions allow chaining without await:
```csharp
await asyncResult.ThenAsync(x => ...).Then(x => ...)
```

**Context propagation**: Wide event context flows through all transformations via `WithContext()` and internal `_context` field.
