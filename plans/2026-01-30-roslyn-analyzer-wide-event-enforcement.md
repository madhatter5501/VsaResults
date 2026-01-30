# Roslyn Analyzer: Wide Event Enforcement

## Files to Create

- [x] `src/VsaResults.Analyzers/VsaResults.Analyzers.csproj`
- [x] `src/VsaResults.Analyzers/DiagnosticDescriptors.cs`
- [x] `src/VsaResults.Analyzers/KnownTypes.cs`
- [x] `src/VsaResults.Analyzers/WideEventEnforcementAnalyzer.cs`
- [x] `src/VsaResults.Analyzers/build/VsaResults.Features.props`
- [x] `tests/Analyzers/Tests.Analyzers.csproj`
- [x] `tests/Analyzers/WideEventEnforcementAnalyzerTests.cs`

## Files to Modify

- [x] `src/VsaResults.Features/VsaResults.Features.csproj` — pack analyzer DLL + props
- [x] `VsaResults.sln` — add new projects
- [x] `src/VsaResults.csproj` — exclude Analyzers folder from compilation glob
- [x] `tests/Tests.csproj` — exclude Analyzers folder from compilation glob

## Verification

1. [x] `dotnet build` — solution builds (0 errors, 4 RS2008 warnings expected)
2. [x] `dotnet test` — all 638 tests pass (8 analyzer + 630 existing)
3. [x] `dotnet pack -c Release` VsaResults.Features — nupkg contains `analyzers/dotnet/cs/VsaResults.Analyzers.dll`, `buildTransitive/VsaResults.Features.props`, `build/VsaResults.Features.props`
