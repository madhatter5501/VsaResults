# VsaResults

A simple, fluent discriminated union of an error or a result for .NET.

## Installation

```bash
dotnet add package VsaResults
```

## Quick Start

```csharp
using VsaResults;

public ErrorOr<int> Divide(int a, int b)
{
    if (b == 0)
        return Error.Validation("Division.ByZero", "Cannot divide by zero");

    return a / b;
}

var result = Divide(10, 2)
    .Then(value => value * 2)
    .Match(
        value => $"Result: {value}",
        errors => $"Error: {errors[0].Description}");
```

## Features

- **Fluent API** - Chain operations with `Then`, `FailIf`, `Else`, `Match`, and `Switch`
- **Multiple Errors** - Collect and return multiple validation errors
- **Async Support** - Full async/await support with `ThenAsync`, `MatchAsync`, etc.
- **Feature Pipeline** - Built-in VSA feature pipeline with `IQueryFeature` and `IMutationFeature`
- **Wide Events** - Structured observability with one log event per feature execution
- **ASP.NET Core Integration** - Convert `ErrorOr` results to `IResult` or `IActionResult`

## API Reference

Browse the [API documentation](api/index.md) for detailed type and method reference.

## Packages

| Package | Description |
|---------|-------------|
| [VsaResults](api/VsaResults.html) | Core library with `ErrorOr<T>`, `Error`, and fluent methods |
| [VsaResults.Messaging](api/VsaResults.Messaging.html) | MassTransit integration with message-wide events |

## Resources

- [GitHub Repository](https://github.com/madhatter5501/ErrorOr)
- [NuGet Package](https://www.nuget.org/packages/VsaResults)
