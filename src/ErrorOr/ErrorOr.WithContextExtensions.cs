namespace VsaResults;

/// <summary>
/// Async extension methods for context accumulation on Task&lt;ErrorOr&lt;T&gt;&gt;.
/// </summary>
public static partial class ErrorOrExtensions
{
    /// <summary>
    /// Adds a key-value pair to the context that flows through the chain.
    /// </summary>
    public static Task<ErrorOr<TValue>> WithContext<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        string key,
        object value) =>
        errorOrTask.ThenSync(errorOr => errorOr.WithContext(key, value));

    /// <summary>
    /// Adds multiple key-value pairs to the context.
    /// </summary>
    public static Task<ErrorOr<TValue>> WithContext<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        params (string Key, object Value)[] pairs) =>
        errorOrTask.ThenSync(errorOr => errorOr.WithContext(pairs));

    /// <summary>
    /// Adds context derived from the success value. Only executes if not in error state.
    /// </summary>
    public static Task<ErrorOr<TValue>> WithContext<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, (string Key, object Value)> selector) =>
        errorOrTask.ThenSync(errorOr => errorOr.WithContext(selector));

    /// <summary>
    /// Adds multiple context entries derived from the success value. Only executes if not in error state.
    /// </summary>
    public static Task<ErrorOr<TValue>> WithContext<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, IEnumerable<(string Key, object Value)>> selector) =>
        errorOrTask.ThenSync(errorOr => errorOr.WithContext(selector));

    /// <summary>
    /// Adds context derived from errors. Only executes if in error state.
    /// </summary>
    public static Task<ErrorOr<TValue>> WithErrorContext<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<List<Error>, (string Key, object Value)> selector) =>
        errorOrTask.ThenSync(errorOr => errorOr.WithErrorContext(selector));

    /// <summary>
    /// Adds context derived from the first error. Only executes if in error state.
    /// </summary>
    public static Task<ErrorOr<TValue>> WithFirstErrorContext<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<Error, (string Key, object Value)> selector) =>
        errorOrTask.ThenSync(errorOr => errorOr.WithFirstErrorContext(selector));
}
