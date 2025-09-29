using EventBusWithTickerQ.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using TickerQ;
using TickerQ.Utilities;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models.Ticker;

namespace EventBusWithTickerQ.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController(ILogger<JobsController> logger, ITimeTickerManager<TimeTicker> timeTickerManager, ICronTickerManager<CronTicker> cronTickerManager) : ControllerBase
{
    [HttpPost("enqueue")]
    public async Task<IActionResult> Enqueue([FromBody] string message)
    {
        // Simulate TickerQ job enqueue
        // Replace with actual TickerQ enqueue logic if needed
        Console.WriteLine($"[Enqueue] {message} at {DateTime.UtcNow:O}");

        await timeTickerManager.AddAsync(new TimeTicker
        {
            Request = TickerHelper.CreateTickerRequest<string>(message),
            ExecutionTime = DateTime.Now.AddSeconds(1),
            Function = "ExampleTicker",
            Description = $"Short Description",
            Retries = 3,
            RetryIntervals = [5, 15, 30] // set in seconds
        });

        return Ok(new { status = "Enqueued", message });
    }

    [HttpPost("delay")]
    public async Task<IActionResult> Delay([FromBody] string message)
    {
        // Simulate delayed job
        Console.WriteLine($"[Delayed] {message} at {DateTime.UtcNow:O}");
        return Ok(new { status = "Delayed", message });
    }

    [HttpPost("retry")]
    public async Task<IActionResult> Retry()
    {
        Console.WriteLine("[Retryable] Throwing to test automatic retries");
        throw new InvalidOperationException("Simulated failure");
    }

    [HttpPost("recurring")]
    public async Task<IActionResult> AddRecurring([FromBody] string cron = "* * * * *")
    {
        Console.WriteLine($"Custom recurring at {DateTime.UtcNow:O} with cron {cron}");

        var result = await cronTickerManager.AddAsync(new CronTicker
        {
            Request = TickerHelper.CreateTickerRequest<string>("Hello Abir Mahmud"),
            Expression = cron,
            Function = "ExampleTicker",
            Description = $"Short Description",
            Retries = 3,
            RetryIntervals = [5, 15, 30] // set in seconds
        });

        return Ok(new { status = "Recurring job set", cron, result.Result.Id });
    }

    [HttpDelete("recurring")]
    public IActionResult RemoveRecurring()
    {
        Console.WriteLine("Recurring job removed");
        return NoContent();
    }
}
