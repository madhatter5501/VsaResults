using FluentAssertions;
using VsaResults;

namespace Tests;

public static class Convert
{
    public static ErrorOr<string> ToString(int num) => num.ToString();

    public static ErrorOr<int> ToInt(string str) => int.Parse(str);

    public static Task<ErrorOr<int>> ToIntAsync(string str) => Task.FromResult(ErrorOrFactory.From(int.Parse(str)));

    public static Task<ErrorOr<string>> ToStringAsync(int num) => Task.FromResult(ErrorOrFactory.From(num.ToString()));
}

public static class ErrorOrAssertions
{
    public static void ShouldBeSuccess<TValue>(this ErrorOr<TValue> result)
    {
        result.IsError.Should().BeFalse();
    }

    public static void ShouldBeError<TValue>(
        this ErrorOr<TValue> result,
        string? code = null,
        ErrorType? type = null)
    {
        result.IsError.Should().BeTrue();

        if (code is not null)
        {
            result.FirstError.Code.Should().Be(code);
        }

        if (type is not null)
        {
            result.FirstError.Type.Should().Be(type);
        }
    }

    public static void ShouldBeValidationError<TValue>(this ErrorOr<TValue> result, string? code = null)
    {
        result.ShouldBeError(code, ErrorType.Validation);
    }
}
