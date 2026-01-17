using FluentAssertions;
using VsaResults;

namespace Tests;

public class ToErrorOrTests
{
    [Fact]
    public void ValueToErrorOr_WhenAccessingValue_ShouldReturnValue()
    {
        // Arrange
        int value = 5;

        // Act
        VsaResult<int> result = value.ToResult();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void ErrorToErrorOr_WhenAccessingFirstError_ShouldReturnSameError()
    {
        // Arrange
        Error error = Error.Unauthorized();

        // Act
        VsaResult<int> result = error.ToResult<int>();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(error);
    }

    [Fact]
    public void ListOfErrorsToErrorOr_WhenAccessingErrors_ShouldReturnSameErrors()
    {
        // Arrange
        List<Error> errors = [Error.Unauthorized(), Error.Validation()];

        // Act
        VsaResult<int> result = errors.ToResult<int>();

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void ArrayOfErrorsToErrorOr_WhenAccessingErrors_ShouldReturnSimilarErrors()
    {
        Error[] errors = [Error.Unauthorized(), Error.Validation()];

        VsaResult<int> result = errors.ToResult<int>();

        result.IsError.Should().BeTrue();
        result.Errors.Should().Equal(errors);
    }
}
