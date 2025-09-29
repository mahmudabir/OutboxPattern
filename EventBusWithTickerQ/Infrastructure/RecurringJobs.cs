namespace EventBusWithTickerQ.Infrastructure;

public static class RecurringJobs
{
    public static void Register()
    {
        // Example recurring jobs demonstrating CRON usage
        Console.WriteLine($"Heartbeat: {DateTime.UtcNow:O}");
        Console.WriteLine("Daily maintenance job executed");
        Console.WriteLine("Weekly report generated");
        Console.WriteLine("Cleanup older data/jobs");
    }
}
