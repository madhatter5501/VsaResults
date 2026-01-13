using FluentAssertions;
using VsaResults;

namespace Tests;

/// <summary>
/// Tests to verify that FeatureHandler methods do not create unnecessary closures.
/// Closure-free delegates are important for performance as they avoid allocations
/// and allow the delegate to be cached by the runtime.
/// </summary>
public class FeatureHandlerClosureTests
{
    public sealed record TestRequest(string Value);

    public sealed record TestResult(string ProcessedValue);

    [Fact]
    public void QueryOk_DoesNotCaptureClosure()
    {
        // Act
        var delegate1 = FeatureHandler.QueryOk<TestRequest, TestResult>();
        var delegate2 = FeatureHandler.QueryOk<TestRequest, TestResult>();

        // Assert - The delegate target should be a static closure (compiler optimization)
        // or if it's a closure class, it should not capture any instance state
        AssertNoCapturedState(delegate1);
        AssertNoCapturedState(delegate2);
    }

    [Fact]
    public void MutationOk_DoesNotCaptureClosure()
    {
        // Act
        var delegate1 = FeatureHandler.MutationOk<TestRequest, TestResult>();
        var delegate2 = FeatureHandler.MutationOk<TestRequest, TestResult>();

        // Assert
        AssertNoCapturedState(delegate1);
        AssertNoCapturedState(delegate2);
    }

    [Fact]
    public void MutationNoContent_DoesNotCaptureClosure()
    {
        // Act
        var delegate1 = FeatureHandler.MutationNoContent<TestRequest>();
        var delegate2 = FeatureHandler.MutationNoContent<TestRequest>();

        // Assert
        AssertNoCapturedState(delegate1);
        AssertNoCapturedState(delegate2);
    }

    [Fact]
    public void Query_WithStaticMapper_DoesNotCaptureClosure()
    {
        // Arrange - Using a static local function means no closure is needed
        static Microsoft.AspNetCore.Http.IResult Mapper(ErrorOr<TestResult> result) =>
            ApiResults.Ok(result);

        // Act
        var handler = FeatureHandler.Query<TestRequest, TestResult>(Mapper);

        // Assert - When using a static local function, the compiler optimizes away the closure
        // The mapper becomes a cached static delegate
        AssertNoCapturedState(handler);
    }

    [Fact]
    public void Mutation_WithStaticMapper_DoesNotCaptureClosure()
    {
        // Arrange
        static Microsoft.AspNetCore.Http.IResult Mapper(ErrorOr<TestResult> result) =>
            ApiResults.Ok(result);

        // Act
        var handler = FeatureHandler.Mutation<TestRequest, TestResult>(Mapper);

        // Assert
        AssertNoCapturedState(handler);
    }

    [Fact]
    public void MutationCreated_WithStaticSelector_DoesNotCaptureClosure()
    {
        // Arrange
        static string LocationSelector(TestResult result) => $"/test/{result.ProcessedValue}";

        // Act
        var handler = FeatureHandler.MutationCreated<TestRequest, TestResult>(LocationSelector);

        // Assert
        AssertNoCapturedState(handler);
    }

    [Fact]
    public void Query_WithInstanceLambda_CapturesTheMapper()
    {
        // Arrange - Using an instance lambda that captures 'this' implicitly via the prefix
        var prefix = "/api";
        Microsoft.AspNetCore.Http.IResult Mapper(ErrorOr<TestResult> result) =>
            result.Match(
                v => Microsoft.AspNetCore.Http.Results.Ok($"{prefix}/{v.ProcessedValue}"),
                e => ApiResults.ToProblem(e));

        // Act
        var handler = FeatureHandler.Query<TestRequest, TestResult>(Mapper);

        // Assert - With a capturing lambda, there will be a closure
        handler.Target.Should().NotBeNull("instance lambda creates a closure");
    }

    [Fact]
    public void MutationCreated_WithInstanceLambda_CapturesTheSelector()
    {
        // Arrange - Using an instance lambda that captures a local variable
        var prefix = "/api/users";
        string LocationSelector(TestResult result) => $"{prefix}/{result.ProcessedValue}";

        // Act
        var handler = FeatureHandler.MutationCreated<TestRequest, TestResult>(LocationSelector);

        // Assert - With a capturing lambda, there will be a closure
        handler.Target.Should().NotBeNull("instance lambda creates a closure");
    }

    [Fact]
    public void QueryOk_MultipleCalls_ReturnFunctionallyEquivalentDelegates()
    {
        // Act
        var delegate1 = FeatureHandler.QueryOk<TestRequest, TestResult>();
        var delegate2 = FeatureHandler.QueryOk<TestRequest, TestResult>();

        // Assert - Both should point to the same method
        delegate1.Method.Should().BeSameAs(delegate2.Method);
    }

    [Fact]
    public void MutationOk_MultipleCalls_ReturnFunctionallyEquivalentDelegates()
    {
        // Act
        var delegate1 = FeatureHandler.MutationOk<TestRequest, TestResult>();
        var delegate2 = FeatureHandler.MutationOk<TestRequest, TestResult>();

        // Assert - Both should point to the same method
        delegate1.Method.Should().BeSameAs(delegate2.Method);
    }

    [Fact]
    public void MutationNoContent_MultipleCalls_ReturnFunctionallyEquivalentDelegates()
    {
        // Act
        var delegate1 = FeatureHandler.MutationNoContent<TestRequest>();
        var delegate2 = FeatureHandler.MutationNoContent<TestRequest>();

        // Assert - Both should point to the same method
        delegate1.Method.Should().BeSameAs(delegate2.Method);
    }

    private static void AssertNoCapturedState(Delegate del)
    {
        // A delegate with no closure either has Target == null (static method)
        // or Target is a compiler-generated singleton closure class with no instance fields
        // that hold user data (only the delegate cache field)
        if (del.Target is null)
        {
            // Static method - no closure at all
            return;
        }

        var targetType = del.Target.GetType();

        // Compiler-generated closure classes have specific naming patterns
        var isCompilerGenerated = targetType.Name.Contains("<>") ||
                                   targetType.Name.Contains("DisplayClass") ||
                                   targetType.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length > 0;

        if (!isCompilerGenerated)
        {
            // If it's not compiler-generated, it shouldn't have instance state
            Assert.Fail($"Unexpected delegate target type: {targetType.Name}");
        }

        // Check that no fields capture external state (fields should only be delegate caches)
        var instanceFields = targetType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        foreach (var field in instanceFields)
        {
            // Delegate cache fields are acceptable
            if (field.FieldType.IsSubclassOf(typeof(Delegate)) || field.FieldType == typeof(Delegate))
            {
                continue;
            }

            // If there are other fields, they should not hold user data
            // (i.e., no closure over local variables)
            var value = field.GetValue(del.Target);
            if (value is not null && !IsStaticClosureSingleton(value))
            {
                Assert.Fail($"Delegate captures state in field '{field.Name}' of type '{field.FieldType.Name}' with value '{value}'");
            }
        }
    }

    private static void AssertOnlyCapturesSpecificField(Delegate del, string expectedFieldName)
    {
        del.Target.Should().NotBeNull("delegates with mappers should have a target");

        var targetType = del.Target!.GetType();
        var instanceFields = targetType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        var capturedFields = instanceFields
            .Where(f => !f.FieldType.IsSubclassOf(typeof(Delegate)) && f.FieldType != typeof(Delegate))
            .Where(f => f.GetValue(del.Target) is not null)
            .ToList();

        // Should only capture the expected mapper/selector field
        capturedFields.Should().ContainSingle(
            $"should only capture '{expectedFieldName}', but found: {string.Join(", ", capturedFields.Select(f => f.Name))}");

        capturedFields[0].Name.Should().Contain(expectedFieldName);
    }

    private static bool IsStaticClosureSingleton(object value)
    {
        // Check if this is a compiler-generated static closure singleton
        var type = value.GetType();
        return type.Name.Contains("<>") && type.GetField("<>9") != null;
    }
}
