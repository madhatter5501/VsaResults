namespace VsaResults;

public static partial class ErrorOrExtensions
{
    /// <summary>
    /// If the state of <paramref name="errorOr"/> is a value, the provided function <paramref name="onValue"/> is executed and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <typeparam name="TNextValue">The type of the result from invoking the <paramref name="onValue"/> function.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onValue">The function to execute if the state is a value.</param>
    /// <returns>The result from calling <paramref name="onValue"/> if state is value; otherwise the original errors.</returns>
    public static Task<ErrorOr<TNextValue>> Then<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Func<TValue, ErrorOr<TNextValue>> onValue) =>
        errorOr.ThenSync(result => result.Then(onValue));

    /// <summary>
    /// If the state of <paramref name="errorOr"/> is a value, the provided function <paramref name="onValue"/> is executed and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <typeparam name="TNextValue">The type of the result from invoking the <paramref name="onValue"/> function.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onValue">The function to execute if the state is a value.</param>
    /// <returns>The result from calling <paramref name="onValue"/> if state is value; otherwise the original errors.</returns>
    public static Task<ErrorOr<TNextValue>> Then<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Func<TValue, TNextValue> onValue) =>
        errorOr.ThenSync(result => result.Then(onValue));

    /// <summary>
    /// If the state of <paramref name="errorOr"/> is a value, the provided <paramref name="action"/> is invoked.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="action">The action to execute if the state is a value.</param>
    /// <returns>The original <paramref name="errorOr"/>.</returns>
    public static Task<ErrorOr<TValue>> ThenDo<TValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Action<TValue> action) =>
        errorOr.ThenSync(result => result.ThenDo(action));

    /// <summary>
    /// If the state of <paramref name="errorOr"/> is a value, the provided function <paramref name="onValue"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <typeparam name="TNextValue">The type of the result from invoking the <paramref name="onValue"/> function.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onValue">The function to execute if the state is a value.</param>
    /// <returns>The result from calling <paramref name="onValue"/> if state is value; otherwise the original errors.</returns>
    public static Task<ErrorOr<TNextValue>> ThenAsync<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Func<TValue, Task<ErrorOr<TNextValue>>> onValue) =>
        errorOr.ThenAsync(result => result.ThenAsync(onValue));

    /// <summary>
    /// If the state of <paramref name="errorOr"/> is a value, the provided function <paramref name="onValue"/> is executed asynchronously and its result is returned.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <typeparam name="TNextValue">The type of the result from invoking the <paramref name="onValue"/> function.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="onValue">The function to execute if the state is a value.</param>
    /// <returns>The result from calling <paramref name="onValue"/> if state is value; otherwise the original errors.</returns>
    public static Task<ErrorOr<TNextValue>> ThenAsync<TValue, TNextValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Func<TValue, Task<TNextValue>> onValue) =>
        errorOr.ThenAsync(result => result.ThenAsync(onValue));

    /// <summary>
    /// If the state of <paramref name="errorOr"/> is a value, the provided <paramref name="action"/> is executed asynchronously.
    /// </summary>
    /// <typeparam name="TValue">The type of the underlying value in the <paramref name="errorOr"/>.</typeparam>
    /// <param name="errorOr">The <see cref="ErrorOr"/> instance.</param>
    /// <param name="action">The action to execute if the state is a value.</param>
    /// <returns>The original <paramref name="errorOr"/>.</returns>
    public static Task<ErrorOr<TValue>> ThenDoAsync<TValue>(
        this Task<ErrorOr<TValue>> errorOr,
        Func<TValue, Task> action) =>
        errorOr.ThenAsync(result => result.ThenDoAsync(action));
}
