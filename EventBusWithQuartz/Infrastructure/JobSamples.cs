using Quartz;

namespace EventBusWithQuartz.Infrastructure;

public static class JobSamples
{
    public static Task EnqueueSample(string message)
    {
        Console.WriteLine($"[Enqueue] {message} at {DateTime.UtcNow:O}");
        return Task.CompletedTask;
    }

    public static Task DelayedSample(string message)
    {
        Console.WriteLine($"[Delayed] {message} at {DateTime.UtcNow:O}");
        return Task.CompletedTask;
    }

    public static Task RetryableSample()
    {
        Console.WriteLine("[Retryable] Throwing to test manual retries via controller");
        throw new InvalidOperationException("Simulated failure");
    }
}

public class DelegateInvocationJob : IJob
{
    public const string MethodNameKey = "method";
    public const string ArgKey = "arg";

    public async Task Execute(IJobExecutionContext context)
    {
        var method = context.MergedJobDataMap.GetString(MethodNameKey)!;
        var arg = context.MergedJobDataMap.GetString(ArgKey);

        switch (method)
        {
            case nameof(JobSamples.EnqueueSample):
                await JobSamples.EnqueueSample(arg!);
                break;
            case nameof(JobSamples.DelayedSample):
                await JobSamples.DelayedSample(arg!);
                break;
            case nameof(JobSamples.RetryableSample):
                await JobSamples.RetryableSample();
                break;
        }
    }
}
