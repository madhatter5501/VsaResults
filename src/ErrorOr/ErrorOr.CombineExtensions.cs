namespace ErrorOr;

public static partial class ErrorOrExtensions
{
    /// <summary>
    /// Combines two async ErrorOr results. Returns a tuple of values if both are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static async Task<ErrorOr<(T1 First, T2 Second)>> Combine<T1, T2>(
        Task<ErrorOr<T1>> firstTask,
        Task<ErrorOr<T2>> secondTask)
    {
        await Task.WhenAll(firstTask, secondTask).ConfigureAwait(false);
        return ErrorOrCombine.Combine(firstTask.Result, secondTask.Result);
    }

    /// <summary>
    /// Combines three async ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static async Task<ErrorOr<(T1 First, T2 Second, T3 Third)>> Combine<T1, T2, T3>(
        Task<ErrorOr<T1>> firstTask,
        Task<ErrorOr<T2>> secondTask,
        Task<ErrorOr<T3>> thirdTask)
    {
        await Task.WhenAll(firstTask, secondTask, thirdTask).ConfigureAwait(false);
        return ErrorOrCombine.Combine(firstTask.Result, secondTask.Result, thirdTask.Result);
    }

    /// <summary>
    /// Combines four async ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static async Task<ErrorOr<(T1 First, T2 Second, T3 Third, T4 Fourth)>> Combine<T1, T2, T3, T4>(
        Task<ErrorOr<T1>> firstTask,
        Task<ErrorOr<T2>> secondTask,
        Task<ErrorOr<T3>> thirdTask,
        Task<ErrorOr<T4>> fourthTask)
    {
        await Task.WhenAll(firstTask, secondTask, thirdTask, fourthTask).ConfigureAwait(false);
        return ErrorOrCombine.Combine(firstTask.Result, secondTask.Result, thirdTask.Result, fourthTask.Result);
    }

    /// <summary>
    /// Collects all results from a sequence of async ErrorOr instances.
    /// Returns all values if all are successful, or all accumulated errors if any failed.
    /// </summary>
    public static async Task<ErrorOr<List<TValue>>> CollectAsync<TValue>(IEnumerable<Task<ErrorOr<TValue>>> tasks)
    {
        var taskArray = tasks.ToArray();
        await Task.WhenAll(taskArray).ConfigureAwait(false);
        return ErrorOrCombine.Collect(taskArray.Select(t => t.Result));
    }

    /// <summary>
    /// Collects all results from an array of async ErrorOr instances.
    /// Returns all values if all are successful, or all accumulated errors if any failed.
    /// </summary>
    public static Task<ErrorOr<List<TValue>>> CollectAsync<TValue>(params Task<ErrorOr<TValue>>[] tasks)
    {
        return CollectAsync((IEnumerable<Task<ErrorOr<TValue>>>)tasks);
    }
}
