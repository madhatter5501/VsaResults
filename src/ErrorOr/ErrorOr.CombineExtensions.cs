namespace VsaResults;

public static partial class ErrorOrExtensions
{
    /// <summary>
    /// Combines two async ErrorOr results. Returns a tuple of values if both are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static Task<ErrorOr<(T1 First, T2 Second)>> Combine<T1, T2>(
        Task<ErrorOr<T1>> firstTask,
        Task<ErrorOr<T2>> secondTask) =>
        Task.WhenAll(firstTask, secondTask)
            .ContinueWith(
                _ => ErrorOrCombine.Combine(firstTask.Result, secondTask.Result),
                TaskContinuationOptions.ExecuteSynchronously);

    /// <summary>
    /// Combines three async ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static Task<ErrorOr<(T1 First, T2 Second, T3 Third)>> Combine<T1, T2, T3>(
        Task<ErrorOr<T1>> firstTask,
        Task<ErrorOr<T2>> secondTask,
        Task<ErrorOr<T3>> thirdTask) =>
        Task.WhenAll(firstTask, secondTask, thirdTask)
            .ContinueWith(
                _ => ErrorOrCombine.Combine(firstTask.Result, secondTask.Result, thirdTask.Result),
                TaskContinuationOptions.ExecuteSynchronously);

    /// <summary>
    /// Combines four async ErrorOr results. Returns a tuple of values if all are successful,
    /// or all accumulated errors if any failed.
    /// </summary>
    public static Task<ErrorOr<(T1 First, T2 Second, T3 Third, T4 Fourth)>> Combine<T1, T2, T3, T4>(
        Task<ErrorOr<T1>> firstTask,
        Task<ErrorOr<T2>> secondTask,
        Task<ErrorOr<T3>> thirdTask,
        Task<ErrorOr<T4>> fourthTask) =>
        Task.WhenAll(firstTask, secondTask, thirdTask, fourthTask)
            .ContinueWith(
                _ => ErrorOrCombine.Combine(firstTask.Result, secondTask.Result, thirdTask.Result, fourthTask.Result),
                TaskContinuationOptions.ExecuteSynchronously);

    /// <summary>
    /// Collects all results from a sequence of async ErrorOr instances.
    /// Returns all values if all are successful, or all accumulated errors if any failed.
    /// </summary>
    public static Task<ErrorOr<List<TValue>>> CollectAsync<TValue>(IEnumerable<Task<ErrorOr<TValue>>> tasks)
    {
        var taskArray = tasks.ToArray();
        return Task.WhenAll(taskArray)
            .ContinueWith(
                _ => ErrorOrCombine.Collect(taskArray.Select(t => t.Result)),
                TaskContinuationOptions.ExecuteSynchronously);
    }

    /// <summary>
    /// Collects all results from an array of async ErrorOr instances.
    /// Returns all values if all are successful, or all accumulated errors if any failed.
    /// </summary>
    public static Task<ErrorOr<List<TValue>>> CollectAsync<TValue>(params Task<ErrorOr<TValue>>[] tasks) =>
        CollectAsync((IEnumerable<Task<ErrorOr<TValue>>>)tasks);
}
