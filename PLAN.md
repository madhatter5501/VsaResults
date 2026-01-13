# Plan: Open Source Best Practices for ErrorOr

## Overview

Enhance ErrorOr repository to follow open source best practices, using the Factory repository as a reference template. This involves adding community files, security documentation, GitHub automation, and enhanced CI/CD workflows.

## User Preferences

- **NuGet Publishing**: Yes, include automated NuGet publish on version tags
- **Code of Conduct**: Contributor Covenant v2.1 (industry standard)
- **Security Policy**: Comprehensive (~200 lines, like Factory)

---

## Files to Create

### 1. Security & Community Files (Root)

| File | Description | Priority |
|------|-------------|----------|
| `SECURITY.md` | Vulnerability reporting, supported versions, security architecture | High |
| `CODE_OF_CONDUCT.md` | Contributor Covenant (referenced in CONTRIBUTING.md but missing) | High |

### 2. GitHub Issue Templates

| File | Description |
|------|-------------|
| `.github/ISSUE_TEMPLATE/bug_report.yml` | Structured bug report with .NET version, OS, reproduction steps |
| `.github/ISSUE_TEMPLATE/feature_request.yml` | Feature requests with problem statement, proposed solution |

### 3. GitHub Configuration Files

| File | Description |
|------|-------------|
| `.github/PULL_REQUEST_TEMPLATE.md` | PR checklist with testing, code quality items |
| `.github/CODEOWNERS` | Code ownership for review routing |
| `.github/dependabot.yml` | Automated NuGet and GitHub Actions updates |
| `.github/labeler.yml` | PR labeling rules based on changed files |

### 4. GitHub Workflows

| File | Description |
|------|-------------|
| `.github/workflows/ci.yml` | Enhanced CI: multi-OS matrix, lint, test, build, release, security |
| `.github/workflows/codeql.yml` | CodeQL security analysis for C# |
| `.github/workflows/stale.yml` | Auto-close stale issues/PRs |
| `.github/workflows/welcome.yml` | Greet first-time contributors |
| `.github/workflows/pr-labeler.yml` | Auto-label PRs by changed files |
| `.github/workflows/release.yml` | NuGet publish on version tags |

---

## Detailed Implementation

### Phase 1: Security & Community (Essential)

#### SECURITY.md
Adapted for .NET/NuGet ecosystem:
- Supported versions table (3.x supported, <3.0 not)
- Vulnerability reporting via GitHub Security Advisories
- Response timeline SLAs
- Basic security considerations for the library

#### CODE_OF_CONDUCT.md
- Contributor Covenant v2.1 (standard)
- Contact method via GitHub issues

---

### Phase 2: GitHub Templates

#### Bug Report Template
Fields:
- Description (required)
- Steps to reproduce (required)
- Expected behavior (required)
- Actual behavior
- .NET version dropdown (.NET 10, .NET 8, .NET 6, Other)
- OS dropdown (Windows, macOS, Linux)
- ErrorOr version
- Code sample
- Auto-labels: `bug`, `triage`

#### Feature Request Template
Fields:
- Problem statement (required)
- Proposed solution (required)
- Alternatives considered
- Component dropdown (Core ErrorOr, AspNetCore, Features, Serialization, Other)
- Auto-labels: `enhancement`

#### Pull Request Template
Sections:
- Description
- Related issue
- Type of change checkboxes
- Changes made list
- Testing checklist
- Code quality checklist (build passes, tests pass, docs updated)

---

### Phase 3: CI/CD Enhancement

#### Replace build.yml with ci.yml
Current limitations:
- Single OS (windows-latest only)
- No security scanning
- No release automation
- No multi-.NET version matrix

New structure:
```
Jobs:
1. lint - Format check, analyzer warnings
2. test - Multi-OS matrix (ubuntu, windows, macos), multi-SDK (.NET 8, .NET 10)
3. build - Release build with artifacts
4. security - CodeQL analysis (separate workflow)
5. release - NuGet publish on v* tags
```

#### CodeQL Workflow
- Scheduled weekly + on PR/push
- C# language analysis
- Security-extended queries

#### Release Workflow
- Trigger on `v*` tags
- Pack NuGet package
- Publish to nuget.org
- Create GitHub release with changelog

---

### Phase 4: Automation

#### Dependabot Configuration
```yaml
- package-ecosystem: "nuget"
  schedule: weekly
- package-ecosystem: "github-actions"
  schedule: weekly
```

#### PR Labeler
Labels based on paths:
- `area/core` - src/ErrorOr/*.cs
- `area/aspnetcore` - src/ErrorOr/AspNetCore/**
- `area/features` - src/ErrorOr/Features/**
- `area/serialization` - src/ErrorOr/Serialization/**
- `documentation` - **/*.md
- `tests` - tests/**
- `ci` - .github/**

#### Stale Issue Management
- Issues stale after 60 days, close after 14 more
- PRs stale after 30 days, close after 7 more
- Exempt: `pinned`, `security`, `bug`, `help wanted`

#### Welcome Bot
- Greet first-time PR contributors with checklist
- Greet first-time issue reporters with helpful links

---

## Files Summary

```
error-or/
├── SECURITY.md                          # NEW
├── CODE_OF_CONDUCT.md                   # NEW
└── .github/
    ├── CODEOWNERS                       # NEW
    ├── PULL_REQUEST_TEMPLATE.md         # NEW
    ├── dependabot.yml                   # NEW
    ├── labeler.yml                      # NEW
    ├── ISSUE_TEMPLATE/
    │   ├── bug_report.yml               # NEW
    │   └── feature_request.yml          # NEW
    └── workflows/
        ├── ci.yml                       # REPLACE build.yml
        ├── codeql.yml                   # NEW
        ├── stale.yml                    # NEW
        ├── welcome.yml                  # NEW
        ├── pr-labeler.yml               # NEW
        └── release.yml                  # NEW (NuGet publish)
```

**Total: 14 new/modified files**

---

## Verification

1. **Syntax validation**: All YAML files parse correctly
2. **Workflow testing**:
   - Create a test branch
   - Push to trigger CI workflow
   - Open PR to test labeler, templates
3. **Security policy**: Verify GitHub Security tab shows policy
4. **Dependabot**: Check Settings > Security > Dependabot for activation
5. **Release flow**:
   - Create test tag `v3.0.0-test`
   - Verify NuGet package builds (dry-run first)

---

## Required Secrets

| Secret | Purpose | Required For |
|--------|---------|--------------|
| `NUGET_API_KEY` | Publish packages to nuget.org | release.yml |
| `CODE_COV_TOKEN` | Upload coverage (already exists) | ci.yml |

---

## Implementation Order

1. **SECURITY.md** - Comprehensive security policy
2. **CODE_OF_CONDUCT.md** - Contributor Covenant v2.1
3. **Issue templates** - bug_report.yml, feature_request.yml
4. **PR template** - PULL_REQUEST_TEMPLATE.md
5. **CODEOWNERS** - Review routing
6. **dependabot.yml** - Automated dependency updates
7. **labeler.yml** - PR auto-labeling rules
8. **ci.yml** - Replace build.yml with enhanced multi-OS CI
9. **codeql.yml** - Security scanning
10. **release.yml** - NuGet publish on tags
11. **stale.yml** - Issue lifecycle management
12. **welcome.yml** - First-time contributor greeting
13. **pr-labeler.yml** - Trigger labeler on PRs

---

## Notes

- All workflows adapted from Factory (Go) to .NET ecosystem
- Using `actions/checkout@v4`, `actions/setup-dotnet@v4` for consistency
- NuGet API key required as repository secret: `NUGET_API_KEY`
- Codecov token already exists: `CODE_COV_TOKEN`
- Original `build.yml` will be deleted after ci.yml is confirmed working
