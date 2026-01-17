using FluentAssertions;
using VsaResults;

namespace Tests;

public class ErrorOrInstantiationTests
{
    private record Person(string Name);

    [Fact]
    public void CreateFromFactory_WhenAccessingValue_ShouldReturnValue()
    {
        // Arrange
        IEnumerable<string> value = ["value"];

        // Act
        VsaResult<IEnumerable<string>> errorOrPerson = VsaResultFactory.From(value);

        // Assert
        errorOrPerson.IsError.Should().BeFalse();
        errorOrPerson.Value.Should().BeSameAs(value);
    }

    [Fact]
    public void CreateFromFactory_WhenAccessingErrors_ShouldThrow()
    {
        // Arrange
        IEnumerable<string> value = ["value"];
        VsaResult<IEnumerable<string>> errorOrPerson = VsaResultFactory.From(value);

        // Act
        Func<List<Error>> errors = () => errorOrPerson.Errors;

        // Assert
        errors.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void CreateFromFactory_WhenAccessingErrorsOrEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        IEnumerable<string> value = ["value"];
        VsaResult<IEnumerable<string>> errorOrPerson = VsaResultFactory.From(value);

        // Act
        List<Error> errors = errorOrPerson.ErrorsOrEmptyList;

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateFromFactory_WhenAccessingFirstError_ShouldThrow()
    {
        // Arrange
        IEnumerable<string> value = ["value"];
        VsaResult<IEnumerable<string>> errorOrPerson = VsaResultFactory.From(value);

        // Act
        Func<Error> action = () => errorOrPerson.FirstError;

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void CreateFromValue_WhenAccessingValue_ShouldReturnValue()
    {
        // Arrange
        IEnumerable<string> value = ["value"];

        // Act
        VsaResult<IEnumerable<string>> errorOrPerson = VsaResultFactory.From(value);

        // Assert
        errorOrPerson.IsError.Should().BeFalse();
        errorOrPerson.Value.Should().BeSameAs(value);
    }

    [Fact]
    public void CreateFromValue_WhenAccessingErrors_ShouldThrow()
    {
        // Arrange
        IEnumerable<string> value = ["value"];
        VsaResult<IEnumerable<string>> errorOrPerson = VsaResultFactory.From(value);

        // Act
        Func<List<Error>> action = () => errorOrPerson.Errors;

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void CreateFromValue_WhenAccessingErrorsOrEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        IEnumerable<string> value = ["value"];
        VsaResult<IEnumerable<string>> errorOrPerson = VsaResultFactory.From(value);

        // Act
        List<Error> errors = errorOrPerson.ErrorsOrEmptyList;

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateFromValue_WhenAccessingFirstError_ShouldThrow()
    {
        // Arrange
        IEnumerable<string> value = ["value"];
        VsaResult<IEnumerable<string>> errorOrPerson = VsaResultFactory.From(value);

        // Act
        Func<Error> action = () => errorOrPerson.FirstError;

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void CreateFromErrorList_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        List<Error> errors = new() { Error.Validation("User.Name", "Name is too short") };
        VsaResult<Person> errorOrPerson = VsaResult<Person>.From(errors);

        // Act & Assert
        errorOrPerson.IsError.Should().BeTrue();
        errorOrPerson.Errors.Should().ContainSingle().Which.Should().Be(errors.Single());
    }

    [Fact]
    public void CreateFromErrorList_WhenAccessingErrorsOrEmptyList_ShouldReturnErrorList()
    {
        // Arrange
        List<Error> errors = new() { Error.Validation("User.Name", "Name is too short") };
        VsaResult<Person> errorOrPerson = VsaResult<Person>.From(errors);

        // Act & Assert
        errorOrPerson.IsError.Should().BeTrue();
        errorOrPerson.ErrorsOrEmptyList.Should().ContainSingle().Which.Should().Be(errors.Single());
    }

    [Fact]
    public void CreateFromErrorList_WhenAccessingValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        List<Error> errors = new() { Error.Validation("User.Name", "Name is too short") };
        VsaResult<Person> errorOrPerson = VsaResult<Person>.From(errors);

        // Act
        var act = () => errorOrPerson.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .And.Message.Should().Be("The Value property cannot be accessed when errors have been recorded. Check IsError before accessing Value.");
    }

    [Fact]
    public void ImplicitCastResult_WhenAccessingResult_ShouldReturnValue()
    {
        // Arrange
        Person result = new Person("Amici");

        // Act
        VsaResult<Person> errorOr = result;

        // Assert
        errorOr.IsError.Should().BeFalse();
        errorOr.Value.Should().Be(result);
    }

    [Fact]
    public void ImplicitCastResult_WhenAccessingErrors_ShouldThrow()
    {
        VsaResult<Person> errorOrPerson = new Person("Amichai");

        // Act
        Func<List<Error>> action = () => errorOrPerson.Errors;

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitCastResult_WhenAccessingFirstError_ShouldThrow()
    {
        VsaResult<Person> errorOrPerson = new Person("Amichai");

        // Act
        Func<Error> action = () => errorOrPerson.FirstError;

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitCastPrimitiveResult_WhenAccessingResult_ShouldReturnValue()
    {
        // Arrange
        const int result = 4;

        // Act
        VsaResult<int> errorOrInt = result;

        // Assert
        errorOrInt.IsError.Should().BeFalse();
        errorOrInt.Value.Should().Be(result);
    }

    [Fact]
    public void ImplicitCastErrorOrType_WhenAccessingResult_ShouldReturnValue()
    {
        // Act
        VsaResult<Success> errorOrSuccess = Result.Success;
        VsaResult<Created> errorOrCreated = Result.Created;
        VsaResult<Deleted> errorOrDeleted = Result.Deleted;
        VsaResult<Updated> errorOrUpdated = Result.Updated;

        // Assert
        errorOrSuccess.IsError.Should().BeFalse();
        errorOrSuccess.Value.Should().Be(Result.Success);

        errorOrCreated.IsError.Should().BeFalse();
        errorOrCreated.Value.Should().Be(Result.Created);

        errorOrDeleted.IsError.Should().BeFalse();
        errorOrDeleted.Value.Should().Be(Result.Deleted);

        errorOrUpdated.IsError.Should().BeFalse();
        errorOrUpdated.Value.Should().Be(Result.Updated);
    }

    [Fact]
    public void ImplicitCastSingleError_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        Error error = Error.Validation("User.Name", "Name is too short");

        // Act
        VsaResult<Person> errorOrPerson = error;

        // Assert
        errorOrPerson.IsError.Should().BeTrue();
        errorOrPerson.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void ImplicitCastError_WhenAccessingValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        VsaResult<Person> errorOrPerson = Error.Validation("User.Name", "Name is too short");

        // Act
        var act = () => errorOrPerson.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .And.Message.Should().Be("The Value property cannot be accessed when errors have been recorded. Check IsError before accessing Value.");
    }

    [Fact]
    public void ImplicitCastSingleError_WhenAccessingFirstError_ShouldReturnError()
    {
        // Arrange
        Error error = Error.Validation("User.Name", "Name is too short");

        // Act
        VsaResult<Person> errorOrPerson = error;

        // Assert
        errorOrPerson.IsError.Should().BeTrue();
        errorOrPerson.FirstError.Should().Be(error);
    }

    [Fact]
    public void ImplicitCastErrorList_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        List<Error> errors = new()
        {
            Error.Validation("User.Name", "Name is too short"),
            Error.Validation("User.Age", "User is too young"),
        };

        // Act
        VsaResult<Person> errorOrPerson = errors;

        // Assert
        errorOrPerson.IsError.Should().BeTrue();
        errorOrPerson.Errors.Should().HaveCount(errors.Count).And.BeEquivalentTo(errors);
    }

    [Fact]
    public void ImplicitCastErrorArray_WhenAccessingErrors_ShouldReturnErrorArray()
    {
        // Arrange
        Error[] errors =
        [
            Error.Validation("User.Name", "Name is too short"),
            Error.Validation("User.Age", "User is too young"),
        ];

        // Act
        VsaResult<Person> errorOrPerson = errors;

        // Assert
        errorOrPerson.IsError.Should().BeTrue();
        errorOrPerson.Errors.Should().HaveCount(errors.Length).And.BeEquivalentTo(errors);
    }

    [Fact]
    public void ImplicitCastErrorList_WhenAccessingFirstError_ShouldReturnFirstError()
    {
        // Arrange
        List<Error> errors = new()
        {
            Error.Validation("User.Name", "Name is too short"),
            Error.Validation("User.Age", "User is too young"),
        };

        // Act
        VsaResult<Person> errorOrPerson = errors;

        // Assert
        errorOrPerson.IsError.Should().BeTrue();
        errorOrPerson.FirstError.Should().Be(errors[0]);
    }

    [Fact]
    public void ImplicitCastErrorArray_WhenAccessingFirstError_ShouldReturnFirstError()
    {
        // Arrange
        Error[] errors =
        [
            Error.Validation("User.Name", "Name is too short"),
            Error.Validation("User.Age", "User is too young"),
        ];

        // Act
        VsaResult<Person> errorOrPerson = errors;

        // Assert
        errorOrPerson.IsError.Should().BeTrue();
        errorOrPerson.FirstError.Should().Be(errors[0]);
    }

    [Fact]
    public void CreateErrorOr_WhenUsingEmptyConstructor_ShouldThrow()
    {
        // Act
        Func<VsaResult<int>> action = () => new VsaResult<int>();

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void CreateErrorOr_WhenEmptyErrorsList_ShouldThrow()
    {
        // Act
        Func<VsaResult<int>> errorOrInt = () => new List<Error>();

        // Assert
        var exception = errorOrInt.Should().ThrowExactly<ArgumentException>().Which;
        exception.Message.Should().Be("Cannot create an VsaResult<TValue> from an empty collection of errors. Provide at least one error. (Parameter 'errors')");
        exception.ParamName.Should().Be("errors");
    }

    [Fact]
    public void CreateErrorOr_WhenEmptyErrorsArray_ShouldThrow()
    {
        // Act
        Func<VsaResult<int>> errorOrInt = () => Array.Empty<Error>();

        // Assert
        var exception = errorOrInt.Should().ThrowExactly<ArgumentException>().Which;
        exception.Message.Should().Be("Cannot create an VsaResult<TValue> from an empty collection of errors. Provide at least one error. (Parameter 'errors')");
        exception.ParamName.Should().Be("errors");
    }

    [Fact]
    public void CreateErrorOr_WhenNullIsPassedAsErrorsList_ShouldThrowArgumentNullException()
    {
        Func<VsaResult<int>> act = () => default(List<Error>)!;

        act.Should().ThrowExactly<ArgumentNullException>()
           .And.ParamName.Should().Be("errors");
    }

    [Fact]
    public void CreateErrorOr_WhenNullIsPassedAsErrorsArray_ShouldThrowArgumentNullException()
    {
        Func<VsaResult<int>> act = () => default(Error[])!;

        act.Should().ThrowExactly<ArgumentNullException>()
           .And.ParamName.Should().Be("errors");
    }

    [Fact]
    public void CreateErrorOr_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Func<VsaResult<int?>> act = () => default(int?);

        act.Should().ThrowExactly<ArgumentNullException>()
           .And.ParamName.Should().Be("value");
    }
}
