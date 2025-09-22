using EventBusWithHangfire.Infrastructure;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace EventBusWithHangfire.Controllers;

[ApiController]
[Route("api/jobs")] 
public class JobsController : ControllerBase
{
    [HttpPost("enqueue")]
    public IActionResult Enqueue([FromBody] string message)
    {
        var jobId = BackgroundJob.Enqueue(() => JobSamples.EnqueueSample(message));
        return Ok(new { jobId });
    }

    [HttpPost("delay")]
    public IActionResult Delay([FromBody] string message)
    {
        var jobId = BackgroundJob.Schedule(() => JobSamples.DelayedSample(message), TimeSpan.FromSeconds(10));
        return Ok(new { jobId });
    }

    [HttpPost("retry")] 
    public IActionResult Retry()
    {
        var jobId = BackgroundJob.Enqueue(() => JobSamples.RetryableSample());
        return Ok(new { jobId });
    }

    [HttpPost("recurring")] 
    public IActionResult AddRecurring([FromBody] string cron)
    {
        RecurringJob.AddOrUpdate("custom-recurring", () => Console.WriteLine($"Custom recurring at {DateTime.UtcNow:O}"), cron);
        return Ok(new { status = "Recurring job set", cron });
    }

    [HttpDelete("recurring")] 
    public IActionResult RemoveRecurring()
    {
        RecurringJob.RemoveIfExists("custom-recurring");
        return NoContent();
    }
}
