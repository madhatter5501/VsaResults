using FluentAssertions;
using VsaResults;

namespace Tests;

public static class Convert
{
    public static VsaResult<string> ToString(int num) => num.ToString();

    public static VsaResult<int> ToInt(string str) => int.Parse(str);

    public static Task<VsaResult<int>> ToIntAsync(string str) => Task.FromResult(VsaResultFactory.From(int.Parse(str)));

    public static Task<VsaResult<string>> ToStringAsync(int num) => Task.FromResult(VsaResultFactory.From(num.ToString()));
}

public static class ErrorOrAssertions
{
    public static void ShouldBeSuccess<TValue>(this VsaResult<TValue> result)
    {
        result.IsError.Should().BeFalse();
    }

    public static void ShouldBeError<TValue>(
        this VsaResult<TValue> result,
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

    public static void ShouldBeValidationError<TValue>(this VsaResult<TValue> result, string? code = null)
    {
        result.ShouldBeError(code, ErrorType.Validation);
    }
}
