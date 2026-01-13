# Contributing to ErrorOr

First off, thank you for considering contributing to ErrorOr! It's people like you that make ErrorOr such a great library.

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior by opening an issue.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When you create a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples** (code snippets, minimal reproductions)
- **Describe the behavior you observed and what you expected**
- **Include the full exception/error message**
- **Specify your environment** (.NET version, OS, ErrorOr version)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:

- **Use a clear and descriptive title**
- **Provide a detailed description of the proposed functionality**
- **Include code examples** showing how the API would be used
- **Explain why this enhancement would be useful**
- **List any alternative solutions you've considered**

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Follow the coding standards** outlined below
3. **Add tests** for any new functionality
4. **Ensure all tests pass** (`dotnet test`)
5. **Run the build** (`dotnet build`)
6. **Update documentation** as needed (XML docs, README)
7. **Write a clear commit message** following conventional commits

## Development Setup

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/ErrorOr.git
cd ErrorOr

# Add upstream remote
git remote add upstream https://github.com/madhatter5501/ErrorOr.git

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Prerequisites

- .NET 10 SDK (or .NET 8 SDK for compatibility testing)
- An IDE with C# support (Visual Studio, Rider, VS Code with C# extension)

## Coding Standards

### C# Style

- Follow standard C# conventions and Microsoft's [Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- Use the project's `.editorconfig` settings (your IDE should pick these up automatically)
- Enable nullable reference types (`#nullable enable`)
- Prefer expression-bodied members for simple operations
- Use meaningful, descriptive names for variables, methods, and types

### XML Documentation

- Add XML documentation comments to all public APIs
- Include `<summary>`, `<param>`, `<returns>`, and `<example>` tags where appropriate
- Document exceptions that may be thrown with `<exception>`

```csharp
/// <summary>
/// Executes the appropriate function based on the state of the <see cref="ErrorOr{TValue}"/>.
/// </summary>
/// <typeparam name="TResult">The return type of the match functions.</typeparam>
/// <param name="onValue">Function to execute if the state is a value.</param>
/// <param name="onError">Function to execute if the state is an error.</param>
/// <returns>The result of the executed function.</returns>
public TResult Match<TResult>(Func<TValue, TResult> onValue, Func<List<Error>, TResult> onError)
```

### Commit Messages

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style (formatting, etc.)
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `perf`: Performance improvement
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

Examples:
```
feat(ErrorOr): add Select method for LINQ-style transformations
fix(serialization): handle null values in JSON deserialization
docs(readme): update Match examples
test(Then): add async cancellation tests
```

### Testing

- Write unit tests for new functionality using xUnit
- Use FluentAssertions for readable assertions
- Follow the existing test organization pattern
- Ensure tests are deterministic and don't rely on timing
- Use descriptive test names that explain the scenario

```csharp
[Fact]
public void Match_WhenIsSuccess_ShouldExecuteOnValueFunction()
{
    // Arrange
    ErrorOr<int> errorOr = 42;

    // Act
    var result = errorOr.Match(
        value => value * 2,
        errors => -1);

    // Assert
    result.Should().Be(84);
}
```

### Documentation

- Update README.md for user-facing changes
- Add inline comments only for complex logic
- Update CHANGELOG.md for notable changes
- Include examples in XML documentation

## Project Structure

```
error-or/
├── src/
│   └── ErrorOr/                    # Main library
│       ├── ErrorOr.cs              # Core discriminated union
│       ├── ErrorOr.Match.cs        # Pattern matching methods
│       ├── ErrorOr.Then.cs         # Chaining methods
│       ├── ErrorOr.Else.cs         # Fallback methods
│       ├── AspNetCore/             # ASP.NET Core integration
│       ├── Features/               # Feature execution pipeline
│       ├── Serialization/          # JSON serialization
│       └── WideEvents/             # Structured logging
├── tests/                          # Test project
├── samples/                        # Sample applications
└── assets/                         # Icons and images
```

### Key Types

When contributing, be aware of these key types:

- `ErrorOr<TValue>` - The main discriminated union struct
- `Error` - Represents an error with code, description, and type
- `ErrorType` - Enum for categorizing errors (Validation, NotFound, etc.)
- `IErrorOr<TValue>` - Interface for the ErrorOr type

## Review Process

1. All PRs require at least one approval
2. CI must pass (build, tests, coverage)
3. Breaking changes require discussion in an issue first
4. Large changes should be broken into smaller PRs when possible
5. Maintain or improve code coverage

## Breaking Changes

If your change modifies public API:

1. Open an issue first to discuss the change
2. Document the breaking change clearly in the PR description
3. Update the CHANGELOG.md with migration guidance
4. Consider providing a deprecation path when possible

## Questions?

Feel free to open an issue with the `question` label or start a discussion.

Thank you for contributing!
