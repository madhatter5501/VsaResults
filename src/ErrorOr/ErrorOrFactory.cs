namespace VsaResults;

/// <summary>
/// Provides factory methods for creating instances of <see cref="ErrorOr{TValue}"/>.
/// </summary>
public static class ErrorOrFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="ErrorOr{TValue}"/> with a value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>An instance of <see cref="ErrorOr{TValue}"/> containing the provided value.</returns>
    public static ErrorOr<TValue> From<TValue>(TValue value) =>
        value;

    /// <summary>
    /// Creates a new instance of <see cref="ErrorOr{TValue}"/> with an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="error">The error to wrap.</param>
    /// <returns>An instance of <see cref="ErrorOr{TValue}"/> containing the provided error.</returns>
    public static ErrorOr<TValue> FromError<TValue>(Error error) =>
        error;

    /// <summary>
    /// Creates a new instance of <see cref="ErrorOr{TValue}"/> with a list of errors.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="errors">The errors to wrap.</param>
    /// <returns>An instance of <see cref="ErrorOr{TValue}"/> containing the provided errors.</returns>
    public static ErrorOr<TValue> FromErrors<TValue>(List<Error> errors) =>
        errors;

    /// <summary>
    /// Creates a new instance of <see cref="ErrorOr{TValue}"/> with a list of errors.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="errors">The errors to wrap.</param>
    /// <returns>An instance of <see cref="ErrorOr{TValue}"/> containing the provided errors.</returns>
    public static ErrorOr<TValue> FromErrors<TValue>(params Error[] errors) =>
        errors.ToList();

    /// <summary>
    /// Executes a function and wraps any thrown exception as an Error.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="errorMapper">An optional function to map exceptions to errors.</param>
    /// <returns>The result of the function or an error if an exception was thrown.</returns>
    public static ErrorOr<TValue> Try<TValue>(Func<TValue> func, Func<Exception, Error>? errorMapper = null) =>
        ErrorOr<TValue>.Try(func, errorMapper);

    /// <summary>
    /// Executes an async function and wraps any thrown exception as an Error.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="func">The async function to execute.</param>
    /// <param name="errorMapper">An optional function to map exceptions to errors.</param>
    /// <returns>The result of the function or an error if an exception was thrown.</returns>
    public static Task<ErrorOr<TValue>> TryAsync<TValue>(
        Func<Task<TValue>> func,
        Func<Exception, Error>? errorMapper = null) =>
        ErrorOr<TValue>.TryAsync(func, errorMapper);
}
