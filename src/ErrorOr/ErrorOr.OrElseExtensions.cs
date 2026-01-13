namespace VsaResults;

/// <summary>
/// Extension methods for OrElse operations on Task-wrapped ErrorOr values.
/// </summary>
public static partial class ErrorOrExtensions
{
    /// <summary>
    /// If the state is error, returns the result of invoking the fallback function.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TValue>> OrElse<TValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Func<List<Error>, ErrorOr<TValue>> fallback) =>
        errorOr.ThenSync(result => result.OrElse(fallback));

    /// <summary>
    /// If the state is error, returns the provided fallback ErrorOr.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TValue>> OrElse<TValue>(
        this Task<ErrorOr<TValue>> errorOr,
        ErrorOr<TValue> fallback) =>
        errorOr.ThenSync(result => result.OrElse(fallback));

    /// <summary>
    /// If the state is error, returns the result of invoking the fallback function based on the first error.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TValue>> OrElseFirst<TValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Func<Error, ErrorOr<TValue>> fallback) =>
        errorOr.ThenSync(result => result.OrElseFirst(fallback));

    /// <summary>
    /// If the state is error, returns the result of invoking the async fallback function.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TValue>> OrElseAsync<TValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Func<List<Error>, Task<ErrorOr<TValue>>> fallback) =>
        errorOr.ThenAsync(result => result.OrElseAsync(fallback));

    /// <summary>
    /// If the state is error, awaits and returns the provided fallback Task.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TValue>> OrElseAsync<TValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Task<ErrorOr<TValue>> fallback) =>
        errorOr.ThenAsync(result => result.OrElseAsync(fallback));

    /// <summary>
    /// If the state is error, returns the result of invoking the async fallback function based on the first error.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TValue>> OrElseFirstAsync<TValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Func<Error, Task<ErrorOr<TValue>>> fallback) =>
        errorOr.ThenAsync(result => result.OrElseFirstAsync(fallback));
}
