using Quartz;

namespace EventBusWithQuartz.Infrastructure;

public static class RecurringJobs
{
    public static async Task RegisterAsync(IScheduler scheduler, CancellationToken ct = default)
    {
        // Heartbeat every minute
        await ScheduleRecurring(scheduler, "heartbeat", "0 0/1 * * * ?", () => Console.WriteLine($"Heartbeat: {DateTime.UtcNow:O}"), ct);
        // Daily maintenance at 02:00 UTC
        await ScheduleRecurring(scheduler, "daily-maintenance", "0 0 2 * * ?", () => Console.WriteLine("Daily maintenance job executed"), ct);
        // Weekly report Monday 09:30 UTC
        await ScheduleRecurring(scheduler, "weekly-report", "0 30 9 ? * MON", () => Console.WriteLine("Weekly report generated"), ct);
        // Hourly cleanup
        await ScheduleRecurring(scheduler, "cleanup-old-jobs", "0 0 0/1 * * ?", () => Console.WriteLine("Cleanup older data/jobs"), ct);
    }

    private static async Task ScheduleRecurring(IScheduler scheduler, string name, string cron, Action action, CancellationToken ct)
    {
        var job = JobBuilder.Create<InlineActionJob>()
            .WithIdentity(name, "recurring")
            .UsingJobData("actionKey", name) // placeholder
            .Build();

        InlineActionJob.Register(name, action);

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trg-{name}", "recurring")
            .WithCronSchedule(cron, x => x.InTimeZone(TimeZoneInfo.Utc))
            .ForJob(job)
            .Build();

        if (await scheduler.CheckExists(job.Key, ct))
        {
            await scheduler.DeleteJob(job.Key, ct);
        }
        await scheduler.ScheduleJob(job, trigger, ct);
    }
}

public class InlineActionJob : IJob
{
    private static readonly Dictionary<string, Action> _actions = new();
    private static readonly object _lock = new();

    public static void Register(string key, Action action)
    {
        lock (_lock)
        {
            _actions[key] = action;
        }
    }

    public Task Execute(IJobExecutionContext context)
    {
        var key = context.JobDetail.Key.Name;
        Action? action = null;
        lock (_lock)
        {
            _actions.TryGetValue(key, out action);
        }
        action?.Invoke();
        return Task.CompletedTask;
    }
}
