namespace VsaResults;

public readonly partial record struct ErrorOr<TValue> : IErrorOr<TValue>
{
    /// <summary>
    /// If the state is error, returns the result of invoking the fallback function which produces a new ErrorOr.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    /// <param name="fallback">The function to invoke if in error state.</param>
    /// <returns>The original ErrorOr if successful, or the result of the fallback function.</returns>
    /// <remarks>
    /// Unlike <see cref="Else(Func{List{Error}, TValue})"/> which recovers to a value,
    /// OrElse allows chaining to another ErrorOr, enabling recovery strategies that might also fail.
    /// </remarks>
    public ErrorOr<TValue> OrElse(Func<List<Error>, ErrorOr<TValue>> fallback) =>
        IsError ? fallback(_errors) : this;

    /// <summary>
    /// If the state is error, returns the provided fallback ErrorOr.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    /// <param name="fallback">The fallback ErrorOr to return if in error state.</param>
    /// <returns>The original ErrorOr if successful, or the fallback ErrorOr.</returns>
    public ErrorOr<TValue> OrElse(ErrorOr<TValue> fallback) =>
        IsError ? fallback : this;

    /// <summary>
    /// If the state is error, returns the result of invoking the fallback function based on the first error.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    /// <param name="fallback">The function to invoke with the first error if in error state.</param>
    /// <returns>The original ErrorOr if successful, or the result of the fallback function.</returns>
    public ErrorOr<TValue> OrElseFirst(Func<Error, ErrorOr<TValue>> fallback) =>
        IsError ? fallback(_errors[0]) : this;

    /// <summary>
    /// If the state is error, returns the result of invoking the async fallback function.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    /// <param name="fallback">The async function to invoke if in error state.</param>
    /// <returns>The original ErrorOr if successful, or the result of the fallback function.</returns>
    public Task<ErrorOr<TValue>> OrElseAsync(Func<List<Error>, Task<ErrorOr<TValue>>> fallback) =>
        IsError
            ? fallback(_errors)
            : Task.FromResult(this);

    /// <summary>
    /// If the state is error, awaits and returns the provided fallback Task.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    /// <param name="fallback">The Task to await and return if in error state.</param>
    /// <returns>The original ErrorOr if successful, or the awaited fallback.</returns>
    public Task<ErrorOr<TValue>> OrElseAsync(Task<ErrorOr<TValue>> fallback) =>
        IsError ? fallback : Task.FromResult(this);

    /// <summary>
    /// If the state is error, returns the result of invoking the async fallback function based on the first error.
    /// If the state is not error, returns the original ErrorOr.
    /// </summary>
    /// <param name="fallback">The async function to invoke with the first error if in error state.</param>
    /// <returns>The original ErrorOr if successful, or the result of the fallback function.</returns>
    public Task<ErrorOr<TValue>> OrElseFirstAsync(Func<Error, Task<ErrorOr<TValue>>> fallback) =>
        IsError
            ? fallback(_errors[0])
            : Task.FromResult(this);
}
