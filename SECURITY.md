# Security Policy

ErrorOr takes security seriously. This document outlines our security practices, vulnerability reporting process, and guidelines for contributors.

## Table of Contents

- [Supported Versions](#supported-versions)
- [Reporting a Vulnerability](#reporting-a-vulnerability)
- [Security Architecture](#security-architecture)
- [Development Security Guidelines](#development-security-guidelines)
- [Dependency Management](#dependency-management)
- [Security Considerations for Users](#security-considerations-for-users)
- [Security Checklist for Contributors](#security-checklist-for-contributors)

---

## Supported Versions

| Version | Supported          | Notes |
| ------- | ------------------ | ----- |
| 3.x.x   | :white_check_mark: | Current stable release |
| < 3.0   | :x:                | No longer maintained |

We provide security updates for the latest major version only. Users are encouraged to stay up-to-date with the latest release.

---

## Reporting a Vulnerability

We appreciate responsible disclosure of security vulnerabilities.

### How to Report

1. **DO NOT** open a public GitHub issue for security vulnerabilities
2. Use GitHub's [Security Advisories](https://github.com/madhatter5501/ErrorOr/security/advisories) feature to privately report vulnerabilities
3. Alternatively, if you cannot use Security Advisories, contact the maintainers directly through GitHub

### What to Include

- Description of the vulnerability
- Steps to reproduce
- Potential impact assessment
- Affected versions
- Suggested fix (if available)

### Response Timeline

| Stage | Timeline |
|-------|----------|
| Initial acknowledgment | Within 48 hours |
| Preliminary assessment | Within 7 days |
| Fix development | Within 30 days (critical), 90 days (moderate) |
| Public disclosure | After fix is released |

### Recognition

We maintain a security acknowledgments section for researchers who responsibly disclose vulnerabilities.

---

## Security Architecture

### Library Design Principles

ErrorOr is designed as a **pure value type library** with minimal attack surface:

```
┌─────────────────────────────────────────────────────────────┐
│                    ErrorOr Library                          │
│                                                             │
│  • No I/O operations                                        │
│  • No network calls                                         │
│  • No file system access                                    │
│  • No reflection-based operations                           │
│  • No dynamic code generation                               │
│  • Immutable data structures                                │
└─────────────────────────────────────────────────────────────┘
```

### Component Security Model

| Component | Security Considerations |
|-----------|------------------------|
| **ErrorOr<T>** | Immutable struct, no side effects |
| **Error** | Immutable record, safe to pass across boundaries |
| **JSON Serialization** | Uses System.Text.Json with safe defaults |
| **ASP.NET Core Extensions** | Follows framework security patterns |
| **Feature Pipeline** | Executes user-provided delegates safely |

### Trust Boundaries

When using ErrorOr in your application:

```
┌─────────────────────────────────────────────────────────────┐
│                    Your Application                          │
│  • Validate all external input before creating Error/Value  │
│  • Sanitize error messages before displaying to users       │
│  • Don't include sensitive data in Error metadata           │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                    ErrorOr Library                          │
│  • Passes data through without modification                 │
│  • Does not validate content of errors or values            │
│  • Preserves all metadata as provided                       │
└─────────────────────────────────────────────────────────────┘
```

---

## Development Security Guidelines

### Code Analysis

The project uses the following security tooling:

| Tool | Purpose | Configuration |
|------|---------|---------------|
| **StyleCop.Analyzers** | Code style and quality | `.editorconfig` |
| **Nullable Reference Types** | Null safety | Enabled project-wide |
| **Roslyn Analyzers** | Security and quality rules | Directory.Build.props |
| **CodeQL** | Static security analysis | GitHub Actions |

### Secure Coding Practices

#### 1. Avoid Information Disclosure

```csharp
// GOOD: Generic error message for external display
return Error.Validation(
    code: "User.InvalidCredentials",
    description: "Invalid username or password");

// BAD: Reveals internal details
return Error.Validation(
    code: "User.InvalidCredentials",
    description: $"User {username} not found in database {connectionString}");
```

#### 2. Safe Error Metadata

```csharp
// GOOD: Include only necessary context
return Error.NotFound(
    code: "Order.NotFound",
    description: "Order not found",
    metadata: new Dictionary<string, object>
    {
        { "orderId", orderId }
    });

// BAD: Including sensitive data in metadata
return Error.NotFound(
    code: "Order.NotFound",
    description: "Order not found",
    metadata: new Dictionary<string, object>
    {
        { "customerSSN", customer.SSN },
        { "creditCard", customer.CreditCard }
    });
```

#### 3. Serialization Safety

When serializing ErrorOr for API responses:

```csharp
// GOOD: Map to a safe DTO before serialization
return errorOr.Match(
    value => Ok(new ApiResponse { Data = value }),
    errors => BadRequest(new ApiError
    {
        Code = errors.First().Code,
        Message = errors.First().Description
        // Don't expose metadata to external clients
    }));
```

---

## Dependency Management

### Dependency Policy

- **Minimal dependencies**: ErrorOr has zero external runtime dependencies
- **Framework dependencies**: Only Microsoft.AspNetCore.* for optional integrations
- **Development dependencies**: StyleCop.Analyzers for code quality

### Dependency Updates

- Dependencies are monitored via Dependabot
- Security updates are applied within 7 days of disclosure
- Breaking changes are evaluated before adoption

### Supply Chain Security

- All packages are restored from nuget.org only
- Package signature verification is enabled
- Lock files are used to ensure reproducible builds

---

## Security Considerations for Users

### Safe Usage Patterns

#### 1. Don't Store Secrets in Errors

```csharp
// BAD: API keys in error messages
return Error.Unexpected(description: $"API call failed with key {apiKey}");

// GOOD: Log separately, return generic message
_logger.LogError("API call failed for key {Key}", apiKey);
return Error.Unexpected(description: "External service unavailable");
```

#### 2. Validate Before Creating ErrorOr

```csharp
// GOOD: Validate input before processing
public ErrorOr<User> CreateUser(CreateUserRequest request)
{
    // Validate and sanitize input first
    if (string.IsNullOrWhiteSpace(request.Email))
        return Error.Validation("User.InvalidEmail", "Email is required");

    var sanitizedEmail = SanitizeEmail(request.Email);
    // ... continue processing
}
```

#### 3. Handle Errors Appropriately by Environment

```csharp
// Development: Full error details
// Production: Sanitized messages only
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var response = app.Environment.IsDevelopment()
            ? new { error = error?.Message, stack = error?.StackTrace }
            : new { error = "An unexpected error occurred" };

        await context.Response.WriteAsJsonAsync(response);
    });
});
```

---

## Security Checklist for Contributors

Before submitting a PR, ensure:

### Code Security

- [ ] No hardcoded secrets, API keys, or credentials
- [ ] No sensitive data logged at INFO level or below
- [ ] Error messages don't reveal internal implementation details
- [ ] All public APIs have XML documentation
- [ ] No use of `dynamic` unless absolutely necessary
- [ ] No unsafe code blocks without justification

### Testing

- [ ] Unit tests don't contain real credentials or PII
- [ ] Test data is clearly synthetic
- [ ] Edge cases for error handling are covered

### Dependencies

- [ ] No new dependencies added without discussion
- [ ] Any new dependencies are from trusted sources
- [ ] Dependencies use specific versions (not floating)

### Documentation

- [ ] Security implications of new features are documented
- [ ] Breaking changes are clearly noted
- [ ] Migration guides mention any security considerations

---

## Security Acknowledgments

We thank the following individuals for responsibly disclosing security issues:

*No security issues have been reported yet.*

---

## Contact

For security-related inquiries that don't fit the vulnerability reporting process, please open a GitHub issue with the `security` label.

---

*Last updated: January 2026*
