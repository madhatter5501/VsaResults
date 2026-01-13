namespace VsaResults;

/// <summary>
/// Provides methods for combining multiple ErrorOr results.
/// </summary>
public static class ErrorOrCombine
{
    /// <summary>
    /// Combines two ErrorOr results. Returns a tuple of values if both are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static ErrorOr<(T1 First, T2 Second)> Combine<T1, T2>(
        ErrorOr<T1> first,
        ErrorOr<T2> second)
    {
        var errors = new List<Error>();

        if (first.IsError)
        {
            errors.AddRange(first.Errors);
        }

        if (second.IsError)
        {
            errors.AddRange(second.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return (first.Value, second.Value);
    }

    /// <summary>
    /// Combines three ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static ErrorOr<(T1 First, T2 Second, T3 Third)> Combine<T1, T2, T3>(
        ErrorOr<T1> first,
        ErrorOr<T2> second,
        ErrorOr<T3> third)
    {
        var errors = new List<Error>();

        if (first.IsError)
        {
            errors.AddRange(first.Errors);
        }

        if (second.IsError)
        {
            errors.AddRange(second.Errors);
        }

        if (third.IsError)
        {
            errors.AddRange(third.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return (first.Value, second.Value, third.Value);
    }

    /// <summary>
    /// Combines four ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static ErrorOr<(T1 First, T2 Second, T3 Third, T4 Fourth)> Combine<T1, T2, T3, T4>(
        ErrorOr<T1> first,
        ErrorOr<T2> second,
        ErrorOr<T3> third,
        ErrorOr<T4> fourth)
    {
        var errors = new List<Error>();

        if (first.IsError)
        {
            errors.AddRange(first.Errors);
        }

        if (second.IsError)
        {
            errors.AddRange(second.Errors);
        }

        if (third.IsError)
        {
            errors.AddRange(third.Errors);
        }

        if (fourth.IsError)
        {
            errors.AddRange(fourth.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return (first.Value, second.Value, third.Value, fourth.Value);
    }

    /// <summary>
    /// Combines five ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static ErrorOr<(T1 First, T2 Second, T3 Third, T4 Fourth, T5 Fifth)> Combine<T1, T2, T3, T4, T5>(
        ErrorOr<T1> first,
        ErrorOr<T2> second,
        ErrorOr<T3> third,
        ErrorOr<T4> fourth,
        ErrorOr<T5> fifth)
    {
        var errors = new List<Error>();

        if (first.IsError)
        {
            errors.AddRange(first.Errors);
        }

        if (second.IsError)
        {
            errors.AddRange(second.Errors);
        }

        if (third.IsError)
        {
            errors.AddRange(third.Errors);
        }

        if (fourth.IsError)
        {
            errors.AddRange(fourth.Errors);
        }

        if (fifth.IsError)
        {
            errors.AddRange(fifth.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return (first.Value, second.Value, third.Value, fourth.Value, fifth.Value);
    }

    /// <summary>
    /// Collects all results from a sequence of ErrorOr instances.
    /// Returns all values if all are successful, or all accumulated errors if any failed.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the ErrorOr instances.</typeparam>
    /// <param name="results">The sequence of ErrorOr instances to collect.</param>
    /// <returns>A list of all values or all accumulated errors.</returns>
    public static ErrorOr<List<TValue>> Collect<TValue>(IEnumerable<ErrorOr<TValue>> results)
    {
        var values = new List<TValue>();
        var errors = new List<Error>();

        foreach (var result in results)
        {
            if (result.IsError)
            {
                errors.AddRange(result.Errors);
            }
            else
            {
                values.Add(result.Value);
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return values;
    }

    /// <summary>
    /// Collects all results from an array of ErrorOr instances.
    /// Returns all values if all are successful, or all accumulated errors if any failed.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the ErrorOr instances.</typeparam>
    /// <param name="results">The array of ErrorOr instances to collect.</param>
    /// <returns>A list of all values or all accumulated errors.</returns>
    public static ErrorOr<List<TValue>> Collect<TValue>(params ErrorOr<TValue>[] results) =>
        Collect((IEnumerable<ErrorOr<TValue>>)results);

    /// <summary>
    /// Combines six ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static ErrorOr<(T1 First, T2 Second, T3 Third, T4 Fourth, T5 Fifth, T6 Sixth)> Combine<T1, T2, T3, T4, T5, T6>(
        ErrorOr<T1> first,
        ErrorOr<T2> second,
        ErrorOr<T3> third,
        ErrorOr<T4> fourth,
        ErrorOr<T5> fifth,
        ErrorOr<T6> sixth)
    {
        var errors = new List<Error>();

        if (first.IsError)
        {
            errors.AddRange(first.Errors);
        }

        if (second.IsError)
        {
            errors.AddRange(second.Errors);
        }

        if (third.IsError)
        {
            errors.AddRange(third.Errors);
        }

        if (fourth.IsError)
        {
            errors.AddRange(fourth.Errors);
        }

        if (fifth.IsError)
        {
            errors.AddRange(fifth.Errors);
        }

        if (sixth.IsError)
        {
            errors.AddRange(sixth.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return (first.Value, second.Value, third.Value, fourth.Value, fifth.Value, sixth.Value);
    }

    /// <summary>
    /// Combines seven ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static ErrorOr<(T1 First, T2 Second, T3 Third, T4 Fourth, T5 Fifth, T6 Sixth, T7 Seventh)> Combine<T1, T2, T3, T4, T5, T6, T7>(
        ErrorOr<T1> first,
        ErrorOr<T2> second,
        ErrorOr<T3> third,
        ErrorOr<T4> fourth,
        ErrorOr<T5> fifth,
        ErrorOr<T6> sixth,
        ErrorOr<T7> seventh)
    {
        var errors = new List<Error>();

        if (first.IsError)
        {
            errors.AddRange(first.Errors);
        }

        if (second.IsError)
        {
            errors.AddRange(second.Errors);
        }

        if (third.IsError)
        {
            errors.AddRange(third.Errors);
        }

        if (fourth.IsError)
        {
            errors.AddRange(fourth.Errors);
        }

        if (fifth.IsError)
        {
            errors.AddRange(fifth.Errors);
        }

        if (sixth.IsError)
        {
            errors.AddRange(sixth.Errors);
        }

        if (seventh.IsError)
        {
            errors.AddRange(seventh.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return (first.Value, second.Value, third.Value, fourth.Value, fifth.Value, sixth.Value, seventh.Value);
    }

    /// <summary>
    /// Combines eight ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static ErrorOr<(T1 First, T2 Second, T3 Third, T4 Fourth, T5 Fifth, T6 Sixth, T7 Seventh, T8 Eighth)> Combine<T1, T2, T3, T4, T5, T6, T7, T8>(
        ErrorOr<T1> first,
        ErrorOr<T2> second,
        ErrorOr<T3> third,
        ErrorOr<T4> fourth,
        ErrorOr<T5> fifth,
        ErrorOr<T6> sixth,
        ErrorOr<T7> seventh,
        ErrorOr<T8> eighth)
    {
        var errors = new List<Error>();

        if (first.IsError)
        {
            errors.AddRange(first.Errors);
        }

        if (second.IsError)
        {
            errors.AddRange(second.Errors);
        }

        if (third.IsError)
        {
            errors.AddRange(third.Errors);
        }

        if (fourth.IsError)
        {
            errors.AddRange(fourth.Errors);
        }

        if (fifth.IsError)
        {
            errors.AddRange(fifth.Errors);
        }

        if (sixth.IsError)
        {
            errors.AddRange(sixth.Errors);
        }

        if (seventh.IsError)
        {
            errors.AddRange(seventh.Errors);
        }

        if (eighth.IsError)
        {
            errors.AddRange(eighth.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return (first.Value, second.Value, third.Value, fourth.Value, fifth.Value, sixth.Value, seventh.Value, eighth.Value);
    }
}
