using Microsoft.Extensions.Logging;

namespace ManagedCode.Orleans.Balancer;

internal static class TaskUtility
{
    internal static async Task RepeatEvery(Func<Task> func,
        TimeSpan interval,
        CancellationToken cancellationToken,
        ILogger<ActivationSheddingFilter> logger)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TaskUtility.RepeatEvery task failed: {ErrorMessage}", ex.Message);
            }

            var task = Task.Delay(interval, cancellationToken);

            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}