namespace ErrorOr;

public static partial class ErrorOrExtensions
{
    /// <summary>
    /// Projects the value using a selector function. This is an alias for ThenAsync.
    /// </summary>
    public static async Task<ErrorOr<TNextValue>> Select<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, TNextValue> selector)
    {
        var errorOr = await errorOrTask.ConfigureAwait(false);
        return errorOr.Select(selector);
    }

    /// <summary>
    /// Projects the value using an async selector function.
    /// </summary>
    public static async Task<ErrorOr<TNextValue>> SelectAsync<TValue, TNextValue>(
        this ErrorOr<TValue> errorOr,
        Func<TValue, Task<TNextValue>> selector)
    {
        if (errorOr.IsError)
        {
            return errorOr.Errors;
        }

        return await selector(errorOr.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Projects the value using an async selector function on a Task-wrapped ErrorOr.
    /// </summary>
    public static async Task<ErrorOr<TNextValue>> SelectAsync<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, Task<TNextValue>> selector)
    {
        var errorOr = await errorOrTask.ConfigureAwait(false);
        return await errorOr.SelectAsync(selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Projects the value using a selector function that returns an ErrorOr. This is an alias for ThenAsync.
    /// </summary>
    public static async Task<ErrorOr<TNextValue>> SelectMany<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, ErrorOr<TNextValue>> selector)
    {
        var errorOr = await errorOrTask.ConfigureAwait(false);
        return errorOr.SelectMany(selector);
    }

    /// <summary>
    /// Projects the value using an async selector function that returns an ErrorOr.
    /// </summary>
    public static async Task<ErrorOr<TNextValue>> SelectManyAsync<TValue, TNextValue>(
        this ErrorOr<TValue> errorOr,
        Func<TValue, Task<ErrorOr<TNextValue>>> selector)
    {
        if (errorOr.IsError)
        {
            return errorOr.Errors;
        }

        return await selector(errorOr.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Projects the value using an async selector function that returns an ErrorOr on a Task-wrapped ErrorOr.
    /// </summary>
    public static async Task<ErrorOr<TNextValue>> SelectManyAsync<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, Task<ErrorOr<TNextValue>>> selector)
    {
        var errorOr = await errorOrTask.ConfigureAwait(false);
        return await errorOr.SelectManyAsync(selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Filters the value based on a predicate on a Task-wrapped ErrorOr.
    /// </summary>
    public static async Task<ErrorOr<TValue>> Where<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, bool> predicate,
        Error error)
    {
        var errorOr = await errorOrTask.ConfigureAwait(false);
        return errorOr.Where(predicate, error);
    }

    /// <summary>
    /// Filters the value based on a predicate on a Task-wrapped ErrorOr.
    /// </summary>
    public static async Task<ErrorOr<TValue>> Where<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, bool> predicate,
        Func<TValue, Error> errorFactory)
    {
        var errorOr = await errorOrTask.ConfigureAwait(false);
        return errorOr.Where(predicate, errorFactory);
    }

    /// <summary>
    /// Filters the value based on an async predicate.
    /// </summary>
    public static async Task<ErrorOr<TValue>> WhereAsync<TValue>(
        this ErrorOr<TValue> errorOr,
        Func<TValue, Task<bool>> predicate,
        Error error)
    {
        if (errorOr.IsError)
        {
            return errorOr.Errors;
        }

        return await predicate(errorOr.Value).ConfigureAwait(false) ? errorOr : error;
    }

    /// <summary>
    /// Filters the value based on an async predicate on a Task-wrapped ErrorOr.
    /// </summary>
    public static async Task<ErrorOr<TValue>> WhereAsync<TValue>(
        this Task<ErrorOr<TValue>> errorOrTask,
        Func<TValue, Task<bool>> predicate,
        Error error)
    {
        var errorOr = await errorOrTask.ConfigureAwait(false);
        return await errorOr.WhereAsync(predicate, error).ConfigureAwait(false);
    }
}
