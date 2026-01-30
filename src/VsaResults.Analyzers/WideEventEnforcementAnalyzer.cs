using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VsaResults.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WideEventEnforcementAnalyzer : DiagnosticAnalyzer
{
    private const string ConfigKey = "build_property.VsaWideEventEnforcement";

    private static readonly ImmutableArray<DiagnosticDescriptor> Diagnostics = ImmutableArray.Create(
        DiagnosticDescriptors.ConsoleOutput,
        DiagnosticDescriptors.ILoggerUsage,
        DiagnosticDescriptors.DebugTraceOutput,
        DiagnosticDescriptors.SerilogStaticCalls);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var severity = GetConfiguredSeverity(compilationContext.Options.AnalyzerConfigOptionsProvider);
            if (severity is null)
                return;

            var knownTypes = new KnownTypes(compilationContext.Compilation);
            if (!knownTypes.HasAnyTypes)
                return;

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeInvocation(nodeContext, knownTypes, severity.Value),
                SyntaxKind.InvocationExpression);
        });
    }

    private static DiagnosticSeverity? GetConfiguredSeverity(AnalyzerConfigOptionsProvider provider)
    {
        if (!provider.GlobalOptions.TryGetValue(ConfigKey, out var value) || string.IsNullOrWhiteSpace(value))
            return null;

        return value!.Trim().ToLowerInvariant() switch
        {
            "warn" => DiagnosticSeverity.Warning,
            "error" => DiagnosticSeverity.Error,
            _ => null,
        };
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        KnownTypes knownTypes,
        DiagnosticSeverity severity)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
            return;

        var containingType = methodSymbol.ContainingType;
        if (containingType is null)
            return;

        DiagnosticDescriptor? descriptor = null;

        // VSA1001: Console output
        if (knownTypes.Console is not null &&
            SymbolEqualityComparer.Default.Equals(containingType, knownTypes.Console) &&
            IsConsoleOutputMethod(methodSymbol.Name))
        {
            descriptor = DiagnosticDescriptors.ConsoleOutput;
        }
        // VSA1003: Debug/Trace output
        else if (knownTypes.Debug is not null &&
                 SymbolEqualityComparer.Default.Equals(containingType, knownTypes.Debug))
        {
            descriptor = DiagnosticDescriptors.DebugTraceOutput;
        }
        else if (knownTypes.Trace is not null &&
                 SymbolEqualityComparer.Default.Equals(containingType, knownTypes.Trace))
        {
            descriptor = DiagnosticDescriptors.DebugTraceOutput;
        }
        // VSA1002: ILogger usage
        else if (IsLoggerCall(methodSymbol, containingType, knownTypes))
        {
            descriptor = DiagnosticDescriptors.ILoggerUsage;
        }
        // VSA1004: Serilog static calls
        else if (knownTypes.SerilogLog is not null &&
                 SymbolEqualityComparer.Default.Equals(containingType, knownTypes.SerilogLog))
        {
            descriptor = DiagnosticDescriptors.SerilogStaticCalls;
        }

        if (descriptor is null)
            return;

        var effectiveDescriptor = DiagnosticDescriptors.WithSeverity(descriptor, severity);
        var memberName = $"{containingType.Name}.{methodSymbol.Name}";
        context.ReportDiagnostic(Diagnostic.Create(effectiveDescriptor, invocation.GetLocation(), memberName));
    }

    private static bool IsConsoleOutputMethod(string methodName) =>
        methodName == "Write" || methodName == "WriteLine";

    private static bool IsLoggerCall(IMethodSymbol method, INamedTypeSymbol containingType, KnownTypes knownTypes)
    {
        // Direct ILogger extension methods (LoggerExtensions.LogInformation, etc.)
        if (knownTypes.LoggerExtensions is not null &&
            SymbolEqualityComparer.Default.Equals(containingType, knownTypes.LoggerExtensions))
        {
            return true;
        }

        // Instance calls on ILogger (logger.Log, etc.)
        if (knownTypes.ILogger is not null && method.Name.StartsWith("Log"))
        {
            if (SymbolEqualityComparer.Default.Equals(containingType, knownTypes.ILogger))
                return true;

            foreach (var iface in containingType.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, knownTypes.ILogger))
                    return true;
            }
        }

        return false;
    }
}
