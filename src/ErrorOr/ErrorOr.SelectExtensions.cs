namespace VsaResults;

public static partial class ErrorOrExtensions
{
    /// <summary>
    /// Projects the value using a selector function. This is an alias for ThenAsync.
    /// </summary>
    public static Task<ErrorOr<TNextValue>> Select<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, TNextValue> selector) =>
        errorOrTask.ThenSync(errorOr => errorOr.Select(selector));

    /// <summary>
    /// Projects the value using an async selector function.
    /// </summary>
    public static Task<ErrorOr<TNextValue>> SelectAsync<TValue, TNextValue>(
        this ErrorOr<TValue> errorOr,
        Func<TValue, Task<TNextValue>> selector) =>
        errorOr.IsError
            ? Task.FromResult<ErrorOr<TNextValue>>(errorOr.Errors)
            : selector(errorOr.Value).ContinueWith(
                t => (ErrorOr<TNextValue>)t.GetAwaiter().GetResult(),
                TaskContinuationOptions.ExecuteSynchronously);

    /// <summary>
    /// Projects the value using an async selector function on a Task-wrapped ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TNextValue>> SelectAsync<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, Task<TNextValue>> selector) =>
        errorOrTask.ThenAsync(errorOr => errorOr.SelectAsync(selector));

    /// <summary>
    /// Projects the value using a selector function that returns an ErrorOr. This is an alias for ThenAsync.
    /// </summary>
    public static Task<ErrorOr<TNextValue>> SelectMany<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, ErrorOr<TNextValue>> selector) =>
        errorOrTask.ThenSync(errorOr => errorOr.SelectMany(selector));

    /// <summary>
    /// Projects the value using an async selector function that returns an ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TNextValue>> SelectManyAsync<TValue, TNextValue>(
        this ErrorOr<TValue> errorOr,
        Func<TValue, Task<ErrorOr<TNextValue>>> selector) =>
        errorOr.IsError
            ? Task.FromResult<ErrorOr<TNextValue>>(errorOr.Errors)
            : selector(errorOr.Value);

    /// <summary>
    /// Projects the value using an async selector function that returns an ErrorOr on a Task-wrapped ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TNextValue>> SelectManyAsync<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, Task<ErrorOr<TNextValue>>> selector) =>
        errorOrTask.ThenAsync(errorOr => errorOr.SelectManyAsync(selector));

    /// <summary>
    /// Filters the value based on a predicate on a Task-wrapped ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TValue>> Where<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, bool> predicate,
        Error error) =>
        errorOrTask.ThenSync(errorOr => errorOr.Where(predicate, error));

    /// <summary>
    /// Filters the value based on a predicate on a Task-wrapped ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TValue>> Where<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, bool> predicate,
        Func<TValue, Error> errorFactory) =>
        errorOrTask.ThenSync(errorOr => errorOr.Where(predicate, errorFactory));

    /// <summary>
    /// Filters the value based on an async predicate.
    /// </summary>
    public static Task<ErrorOr<TValue>> WhereAsync<TValue>(
        this ErrorOr<TValue> errorOr,
        Func<TValue, Task<bool>> predicate,
        Error error) =>
        errorOr.IsError
            ? Task.FromResult<ErrorOr<TValue>>(errorOr.Errors)
            : predicate(errorOr.Value).ContinueWith(
                t => t.GetAwaiter().GetResult() ? errorOr : (ErrorOr<TValue>)error,
                TaskContinuationOptions.ExecuteSynchronously);

    /// <summary>
    /// Filters the value based on an async predicate on a Task-wrapped ErrorOr.
    /// </summary>
    public static Task<ErrorOr<TValue>> WhereAsync<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, Task<bool>> predicate,
        Error error) =>
        errorOrTask.ThenAsync(errorOr => errorOr.WhereAsync(predicate, error));
}
