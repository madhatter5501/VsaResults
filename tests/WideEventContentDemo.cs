using FluentAssertions;
using VsaResults;
using Xunit;
using Xunit.Abstractions;

namespace Tests;

public class WideEventContentDemo
{
    private readonly ITestOutputHelper _output;

    public WideEventContentDemo(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ShowWideEventContent()
    {
        // Create a success wide event
        var builder = FeatureWideEvent.Start("CreateOrder", "Mutation")
            .WithContext("order_id", Guid.Parse("12345678-1234-1234-1234-123456789012"))
            .WithContext("customer_id", "CUST-12345")
            .WithContext("amount", 199.99m);

        builder.StartStage();
        Thread.Sleep(5);
        builder.RecordValidation();

        builder.StartStage();
        Thread.Sleep(10);
        builder.RecordRequirements();

        builder.StartStage();
        Thread.Sleep(15);
        builder.RecordExecution();

        builder.StartStage();
        Thread.Sleep(3);
        builder.RecordSideEffects();

        var successEvent = builder.Success();

        _output.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        _output.WriteLine("║                    SUCCESS WIDE EVENT                        ║");
        _output.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        _output.WriteLine($"FeatureName:     {successEvent.FeatureName}");
        _output.WriteLine($"FeatureType:     {successEvent.FeatureType}");
        _output.WriteLine($"Outcome:         {successEvent.Outcome}");
        _output.WriteLine($"IsSuccess:       {successEvent.IsSuccess}");
        _output.WriteLine($"Timestamp:       {successEvent.Timestamp:O}");
        _output.WriteLine($"Host:            {successEvent.Host}");
        _output.WriteLine(string.Empty);
        _output.WriteLine("┌─ Timing (milliseconds) ────────────────────────────────────┐");
        _output.WriteLine($"│ ValidationMs:    {successEvent.ValidationMs,8:F2}                              │");
        _output.WriteLine($"│ RequirementsMs:  {successEvent.RequirementsMs,8:F2}                              │");
        _output.WriteLine($"│ ExecutionMs:     {successEvent.ExecutionMs,8:F2}                              │");
        _output.WriteLine($"│ SideEffectsMs:   {successEvent.SideEffectsMs,8:F2}                              │");
        _output.WriteLine($"│ TotalMs:         {successEvent.TotalMs,8:F2}                              │");
        _output.WriteLine("└─────────────────────────────────────────────────────────────┘");
        _output.WriteLine(string.Empty);
        _output.WriteLine("┌─ Request Context ──────────────────────────────────────────┐");
        foreach (var kv in successEvent.RequestContext)
        {
            _output.WriteLine($"│   {kv.Key,-15}: {kv.Value,-38} │");
        }

        _output.WriteLine("└─────────────────────────────────────────────────────────────┘");

        // Failure event
        _output.WriteLine(string.Empty);
        _output.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        _output.WriteLine("║                    FAILURE WIDE EVENT                        ║");
        _output.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        var failBuilder = FeatureWideEvent.Start("ValidatePayment", "Mutation");
        var errors = new List<Error>
        {
            Error.Validation("Card.Expired", "Credit card has expired"),
            Error.Validation("Card.Invalid", "Card number is invalid"),
        };
        var failEvent = failBuilder.ValidationFailure(errors);

        _output.WriteLine($"FeatureName:     {failEvent.FeatureName}");
        _output.WriteLine($"Outcome:         {failEvent.Outcome}");
        _output.WriteLine($"IsSuccess:       {failEvent.IsSuccess}");
        _output.WriteLine($"FailedAtStage:   {failEvent.FailedAtStage}");
        _output.WriteLine(string.Empty);
        _output.WriteLine("┌─ Error Details ────────────────────────────────────────────┐");
        _output.WriteLine($"│ ErrorCode:       {failEvent.ErrorCode,-40} │");
        _output.WriteLine($"│ ErrorType:       {failEvent.ErrorType,-40} │");
        _output.WriteLine($"│ ErrorMessage:    {failEvent.ErrorMessage,-40} │");
        _output.WriteLine($"│ ErrorCount:      {failEvent.ErrorCount,-40} │");
        _output.WriteLine("│ ErrorDescription:                                          │");
        _output.WriteLine($"│   {failEvent.ErrorDescription,-56} │");
        _output.WriteLine("└─────────────────────────────────────────────────────────────┘");

        // Exception event - simulate exception during execution stage
        _output.WriteLine(string.Empty);
        _output.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        _output.WriteLine("║                   EXCEPTION WIDE EVENT                       ║");
        _output.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        var exBuilder = FeatureWideEvent.Start("ProcessOrder", "Mutation");

        // Simulate pipeline stages - the stage is tracked when StartStage(stageName, type, method) is called
        exBuilder.StartStage("validation", typeof(NoOpValidator<string>), "ValidateAsync");
        exBuilder.RecordValidation();
        exBuilder.StartStage("requirements", typeof(NoOpRequirements<string>), "EnforceAsync");
        exBuilder.RecordRequirements();
        exBuilder.StartStage("execution", typeof(OrderMutator), "ExecuteAsync"); // Exception happens during execution
        try
        {
            throw new InvalidOperationException("Database connection failed");
        }
        catch (Exception ex)
        {
            var exEvent = exBuilder.Exception(ex);
            _output.WriteLine($"FeatureName:     {exEvent.FeatureName}");
            _output.WriteLine($"Outcome:         {exEvent.Outcome}");
            _output.WriteLine($"FailedAtStage:   {exEvent.FailedAtStage}");
            _output.WriteLine(string.Empty);
            _output.WriteLine("┌─ Exception Details ────────────────────────────────────────┐");
            _output.WriteLine($"│ ExceptionType:   {exEvent.ExceptionType,-40} │");
            _output.WriteLine($"│ ExceptionMessage:{exEvent.ExceptionMessage,-40} │");
            _output.WriteLine("└─────────────────────────────────────────────────────────────┘");
            _output.WriteLine(string.Empty);
            _output.WriteLine("┌─ Failed Component Location ────────────────────────────────┐");
            _output.WriteLine($"│ Namespace: {exEvent.FailedInNamespace,-47} │");
            _output.WriteLine($"│ Class:     {exEvent.FailedInClass,-47} │");
            _output.WriteLine($"│ Method:    {exEvent.FailedInMethod,-47} │");
            _output.WriteLine("└─────────────────────────────────────────────────────────────┘");
        }

        successEvent.IsSuccess.Should().BeTrue();
    }

    // Mock mutator for demo purposes
    private sealed class OrderMutator
    {
    }
}
