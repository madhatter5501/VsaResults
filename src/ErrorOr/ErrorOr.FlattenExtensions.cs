namespace VsaResults;

/// <summary>
/// Extension methods for flattening nested ErrorOr values.
/// </summary>
public static partial class ErrorOrExtensions
{
    /// <summary>
    /// Flattens a nested <see cref="ErrorOr{T}"/> structure.
    /// If the outer ErrorOr is in error state, returns those errors.
    /// If the outer ErrorOr is successful, returns the inner ErrorOr.
    /// </summary>
    /// <typeparam name="TValue">The type of the inner value.</typeparam>
    /// <param name="nested">The nested ErrorOr to flatten.</param>
    /// <returns>A flattened <see cref="ErrorOr{TValue}"/>.</returns>
    /// <example>
    /// <code>
    /// ErrorOr&lt;ErrorOr&lt;int&gt;&gt; nested = GetNestedResult();
    /// ErrorOr&lt;int&gt; flattened = nested.Flatten();
    /// </code>
    /// </example>
    public static ErrorOr<TValue> Flatten<TValue>(this ErrorOr<ErrorOr<TValue>> nested) =>
        nested.IsError ? nested.Errors : nested.Value;

    /// <summary>
    /// Flattens a Task-wrapped nested <see cref="ErrorOr{T}"/> structure.
    /// </summary>
    /// <typeparam name="TValue">The type of the inner value.</typeparam>
    /// <param name="nested">The Task-wrapped nested ErrorOr to flatten.</param>
    /// <returns>A flattened <see cref="ErrorOr{TValue}"/>.</returns>
    public static Task<ErrorOr<TValue>> Flatten<TValue>(this Task<ErrorOr<ErrorOr<TValue>>> nested) =>
        nested.ThenSync(result => result.Flatten());

    /// <summary>
    /// Flattens an ErrorOr containing a Task-wrapped ErrorOr.
    /// </summary>
    /// <typeparam name="TValue">The type of the inner value.</typeparam>
    /// <param name="nested">The ErrorOr containing a Task-wrapped ErrorOr.</param>
    /// <returns>A flattened <see cref="ErrorOr{TValue}"/>.</returns>
    public static Task<ErrorOr<TValue>> FlattenAsync<TValue>(this ErrorOr<Task<ErrorOr<TValue>>> nested) =>
        nested.IsError
            ? Task.FromResult<ErrorOr<TValue>>(nested.Errors)
            : nested.Value;
}
