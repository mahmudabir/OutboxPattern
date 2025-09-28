using EventBusWithQuartz.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace EventBusWithQuartz.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController(ISchedulerFactory schedulerFactory, ILogger<JobsController> logger) : ControllerBase
{
    [HttpPost("enqueue")]
    public async Task<IActionResult> Enqueue([FromBody] string message)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var id = $"enqueue-{Guid.NewGuid():N}";
        var job = JobBuilder.Create<DelegateInvocationJob>()
            .WithIdentity(id, "adhoc")
            .UsingJobData(DelegateInvocationJob.MethodNameKey, nameof(JobSamples.EnqueueSample))
            .UsingJobData(DelegateInvocationJob.ArgKey, message)
            .Build();
        var trigger = TriggerBuilder.Create().ForJob(job).StartNow().Build();
        await scheduler.ScheduleJob(job, trigger);
        return Ok(new { jobId = id });
    }

    [HttpPost("delay")]
    public async Task<IActionResult> Delay([FromBody] string message)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var id = $"delay-{Guid.NewGuid():N}";
        var job = JobBuilder.Create<DelegateInvocationJob>()
            .WithIdentity(id, "adhoc")
            .UsingJobData(DelegateInvocationJob.MethodNameKey, nameof(JobSamples.DelayedSample))
            .UsingJobData(DelegateInvocationJob.ArgKey, message)
            .Build();
        var trigger = TriggerBuilder.Create().ForJob(job).StartAt(DateTimeOffset.UtcNow.AddSeconds(10)).Build();
        await scheduler.ScheduleJob(job, trigger);
        return Ok(new { jobId = id });
    }

    [HttpPost("retry")]
    public async Task<IActionResult> Retry()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var id = $"retry-{Guid.NewGuid():N}";
        var job = JobBuilder.Create<DelegateInvocationJob>()
            .WithIdentity(id, "adhoc")
            .UsingJobData(DelegateInvocationJob.MethodNameKey, nameof(JobSamples.RetryableSample))
            .Build();
        var trigger = TriggerBuilder.Create().ForJob(job).StartNow().Build();
        await scheduler.ScheduleJob(job, trigger);
        return Ok(new { jobId = id });
    }

    [HttpPost("recurring")]
    public async Task<IActionResult> AddRecurring([FromBody] string cron)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var name = "custom-recurring";
        var job = JobBuilder.Create<DelegateInvocationJob>()
            .WithIdentity(name, "recurring")
            .UsingJobData(DelegateInvocationJob.MethodNameKey, nameof(JobSamples.EnqueueSample))
            .UsingJobData(DelegateInvocationJob.ArgKey, "Custom recurring")
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trg-{name}", "recurring")
            .WithCronSchedule(cron, x => x.InTimeZone(TimeZoneInfo.Utc))
            .ForJob(job)
            .Build();
        if (await scheduler.CheckExists(job.Key))
        {
            await scheduler.DeleteJob(job.Key);
        }
        await scheduler.ScheduleJob(job, trigger);
        return Ok(new { status = "Recurring job set", cron });
    }

    [HttpDelete("recurring")]
    public async Task<IActionResult> RemoveRecurring()
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKey = new JobKey("custom-recurring", "recurring");
        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
        }
        return NoContent();
    }
}
