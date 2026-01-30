using Microsoft.CodeAnalysis;

namespace VsaResults.Analyzers;

internal sealed class KnownTypes
{
    public INamedTypeSymbol? Console { get; }
    public INamedTypeSymbol? Debug { get; }
    public INamedTypeSymbol? Trace { get; }
    public INamedTypeSymbol? ILogger { get; }
    public INamedTypeSymbol? LoggerExtensions { get; }
    public INamedTypeSymbol? SerilogLog { get; }

    public KnownTypes(Compilation compilation)
    {
        Console = compilation.GetTypeByMetadataName("System.Console");
        Debug = compilation.GetTypeByMetadataName("System.Diagnostics.Debug");
        Trace = compilation.GetTypeByMetadataName("System.Diagnostics.Trace");
        ILogger = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
        LoggerExtensions = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerExtensions");
        SerilogLog = compilation.GetTypeByMetadataName("Serilog.Log");
    }

    public bool HasAnyTypes =>
        Console is not null ||
        Debug is not null ||
        Trace is not null ||
        ILogger is not null ||
        LoggerExtensions is not null ||
        SerilogLog is not null;
}
