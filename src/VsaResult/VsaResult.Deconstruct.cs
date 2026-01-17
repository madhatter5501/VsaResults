namespace VsaResults;

public readonly partial record struct VsaResult<TValue> : IVsaResult<TValue>
{
    /// <summary>
    /// Deconstructs the ErrorOr into its value and errors components.
    /// </summary>
    /// <param name="value">The value if successful, otherwise default.</param>
    /// <param name="errors">The errors if in error state, otherwise null.</param>
    /// <example>
    /// <code>
    /// var (value, errors) = result;
    /// if (errors is not null)
    /// {
    ///     // handle errors
    /// }
    /// else
    /// {
    ///     // use value
    /// }
    /// </code>
    /// </example>
    public void Deconstruct(out TValue? value, out List<Error>? errors)
    {
        if (IsError)
        {
            value = default;
            errors = _errors;
        }
        else
        {
            value = _value;
            errors = null;
        }
    }

    /// <summary>
    /// Deconstructs the ErrorOr into its state, value, and errors components.
    /// </summary>
    /// <param name="isError">Whether the ErrorOr is in an error state.</param>
    /// <param name="value">The value if successful, otherwise default.</param>
    /// <param name="errors">The errors if in error state, otherwise a new empty list.</param>
    /// <remarks>
    /// Note: When in a non-error state, a new empty list is returned to prevent
    /// accidental mutation of a shared instance.
    /// </remarks>
    /// <example>
    /// <code>
    /// var (isError, value, errors) = result;
    /// if (isError)
    /// {
    ///     foreach (var error in errors)
    ///     {
    ///         Console.WriteLine(error.Description);
    ///     }
    /// }
    /// </code>
    /// </example>
    public void Deconstruct(out bool isError, out TValue? value, out List<Error> errors)
    {
        isError = IsError;

        if (IsError)
        {
            value = default;
            errors = _errors;
        }
        else
        {
            value = _value;
            errors = [];
        }
    }
}
