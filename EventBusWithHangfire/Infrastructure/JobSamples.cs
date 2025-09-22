using Hangfire;

namespace EventBusWithHangfire.Infrastructure;

public static class JobSamples
{
    public static string EnqueueSample(string message)
    {
        Console.WriteLine($"[Enqueue] {message} at {DateTime.UtcNow:O}");
        return message;
    }

    public static void DelayedSample(string message)
    {
        Console.WriteLine($"[Delayed] {message} at {DateTime.UtcNow:O}");
    }

    [AutomaticRetry(Attempts = 3)]
    public static void RetryableSample()
    {
        Console.WriteLine("[Retryable] Throwing to test automatic retries");
        throw new InvalidOperationException("Simulated failure");
    }
}
