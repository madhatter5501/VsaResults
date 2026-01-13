namespace ErrorOr;

public readonly partial record struct ErrorOr<TValue> : IErrorOr<TValue>
{
    /// <summary>
    /// Projects the value using a selector function. This is an alias for <see cref="Then{TNextValue}(Func{TValue, TNextValue})"/>.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the projected value.</typeparam>
    /// <param name="selector">The projection function to apply to the value.</param>
    /// <returns>An ErrorOr containing the projected value or the original errors.</returns>
    public ErrorOr<TNextValue> Select<TNextValue>(Func<TValue, TNextValue> selector)
    {
        return Then(selector);
    }

    /// <summary>
    /// Projects the value using a selector function that returns an ErrorOr.
    /// This is an alias for <see cref="Then{TNextValue}(Func{TValue, ErrorOr{TNextValue}})"/>.
    /// </summary>
    /// <typeparam name="TNextValue">The type of the projected value.</typeparam>
    /// <param name="selector">The projection function that returns an ErrorOr.</param>
    /// <returns>The projected ErrorOr or the original errors.</returns>
    public ErrorOr<TNextValue> SelectMany<TNextValue>(Func<TValue, ErrorOr<TNextValue>> selector)
    {
        return Then(selector);
    }

    /// <summary>
    /// Filters the value based on a predicate, returning the specified error if the predicate fails.
    /// </summary>
    /// <param name="predicate">The predicate to test the value against.</param>
    /// <param name="error">The error to return if the predicate fails.</param>
    /// <returns>The original value if the predicate passes, or the specified error.</returns>
    public ErrorOr<TValue> Where(Func<TValue, bool> predicate, Error error)
    {
        if (IsError)
        {
            return Errors;
        }

        return predicate(Value) ? this : error;
    }

    /// <summary>
    /// Filters the value based on a predicate, returning the error from the error factory if the predicate fails.
    /// </summary>
    /// <param name="predicate">The predicate to test the value against.</param>
    /// <param name="errorFactory">A function that produces the error if the predicate fails.</param>
    /// <returns>The original value if the predicate passes, or the error from the factory.</returns>
    public ErrorOr<TValue> Where(Func<TValue, bool> predicate, Func<TValue, Error> errorFactory)
    {
        if (IsError)
        {
            return Errors;
        }

        return predicate(Value) ? this : errorFactory(Value);
    }
}
