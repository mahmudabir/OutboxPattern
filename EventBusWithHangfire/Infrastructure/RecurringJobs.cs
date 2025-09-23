using Hangfire;

namespace EventBusWithHangfire.Infrastructure;

public static class RecurringJobs
{
    public static void Register()
    {
        // Example recurring jobs demonstrating CRON usage

        // Every minute
        RecurringJob.AddOrUpdate("heartbeat", () => Console.WriteLine($"Heartbeat: {DateTime.UtcNow:O}"), Cron.Minutely);

        // Every day at 02:00 UTC
        RecurringJob.AddOrUpdate("daily-maintenance", () => Console.WriteLine("Daily maintenance job executed"), "0 2 * * *");

        // Every Monday at 09:30 UTC
        RecurringJob.AddOrUpdate("weekly-report", () => Console.WriteLine("Weekly report generated"), "30 9 * * 1");

        // Example: schedule cleanup in low priority queue
        RecurringJob.AddOrUpdate("cleanup-old-jobs",
            () => Console.WriteLine("Cleanup older data/jobs"),
            Cron.Hourly);
    }
}
