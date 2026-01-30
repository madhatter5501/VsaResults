using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using VsaResults.Analyzers;
using Xunit;

namespace Tests.Analyzers;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<VsaResults.Analyzers.WideEventEnforcementAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

public class WideEventEnforcementAnalyzerTests
{
    [Fact]
    public async Task ConsoleWriteLine_WhenEnforcementWarn_ReportsWarning()
    {
        // Arrange
        const string source = """
            using System;

            class Test
            {
                void M()
                {
                    Console.WriteLine("hello");
                }
            }
            """;

        var expected = Verify.Diagnostic(DiagnosticDescriptors.ConsoleOutput)
            .WithLocation(7, 9)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("Console.WriteLine");

        // Act & Assert
        await CreateTest(source, "warn", expected).RunAsync();
    }

    [Fact]
    public async Task ConsoleWrite_WhenEnforcementError_ReportsError()
    {
        // Arrange
        const string source = """
            using System;

            class Test
            {
                void M()
                {
                    Console.Write("hello");
                }
            }
            """;

        var expected = Verify.Diagnostic(DiagnosticDescriptors.ConsoleOutput)
            .WithLocation(7, 9)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("Console.Write");

        // Act & Assert
        await CreateTest(source, "error", expected).RunAsync();
    }

    [Fact]
    public async Task ConsoleWriteLine_WhenEnforcementOff_NoDiagnostic()
    {
        // Arrange
        const string source = """
            using System;

            class Test
            {
                void M()
                {
                    Console.WriteLine("hello");
                }
            }
            """;

        // Act & Assert
        await CreateTest(source, "off").RunAsync();
    }

    [Fact]
    public async Task ConsoleWriteLine_WhenEnforcementEmpty_NoDiagnostic()
    {
        // Arrange
        const string source = """
            using System;

            class Test
            {
                void M()
                {
                    Console.WriteLine("hello");
                }
            }
            """;

        // Act & Assert
        await CreateTest(source, string.Empty).RunAsync();
    }

    [Fact]
    public async Task DebugWriteLine_WhenEnforcementWarn_ReportsWarning()
    {
        // Arrange
        const string source = """
            using System.Diagnostics;

            class Test
            {
                void M()
                {
                    Debug.WriteLine("debug info");
                }
            }
            """;

        var expected = Verify.Diagnostic(DiagnosticDescriptors.DebugTraceOutput)
            .WithLocation(7, 9)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("Debug.WriteLine");

        // Act & Assert
        await CreateTest(source, "warn", expected).RunAsync();
    }

    [Fact]
    public async Task TraceTraceWarning_WhenEnforcementWarn_ReportsWarning()
    {
        // Arrange
        const string source = """
            using System.Diagnostics;

            class Test
            {
                void M()
                {
                    Trace.TraceWarning("warning");
                }
            }
            """;

        var expected = Verify.Diagnostic(DiagnosticDescriptors.DebugTraceOutput)
            .WithLocation(7, 9)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("Trace.TraceWarning");

        // Act & Assert
        await CreateTest(source, "warn", expected).RunAsync();
    }

    [Fact]
    public async Task ConsoleReadLine_WhenEnforcementWarn_NoDiagnostic()
    {
        // Arrange — Console.ReadLine is not a logging call
        const string source = """
            using System;

            class Test
            {
                void M()
                {
                    var input = Console.ReadLine();
                }
            }
            """;

        // Act & Assert
        await CreateTest(source, "warn").RunAsync();
    }

    [Fact]
    public async Task NoLogging_WhenEnforcementWarn_NoDiagnostic()
    {
        // Arrange
        const string source = """
            class Test
            {
                int Add(int a, int b) => a + b;
            }
            """;

        // Act & Assert
        await CreateTest(source, "warn").RunAsync();
    }

    private static AnalyzerTest<DefaultVerifier> CreateTest(
        string source,
        string enforcement = "warn",
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<WideEventEnforcementAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            TestState =
            {
                AnalyzerConfigFiles =
                {
                    ("/.globalconfig", $"""
                        is_global = true
                        build_property.VsaWideEventEnforcement = {enforcement}
                        """),
                },
            },
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test;
    }
}
