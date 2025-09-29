using EventBusWithTickerQ.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using TickerQ;

namespace EventBusWithTickerQ.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController(ILogger<JobsController> logger) : ControllerBase
{
    [HttpPost("enqueue")]
    public IActionResult Enqueue([FromBody] string message)
    {
        // Simulate TickerQ job enqueue
        // Replace with actual TickerQ enqueue logic if needed
        Console.WriteLine($"[Enqueue] {message} at {DateTime.UtcNow:O}");
        return Ok(new { status = "Enqueued", message });
    }

    [HttpPost("delay")]
    public IActionResult Delay([FromBody] string message)
    {
        // Simulate delayed job
        Console.WriteLine($"[Delayed] {message} at {DateTime.UtcNow:O}");
        return Ok(new { status = "Delayed", message });
    }

    [HttpPost("retry")]
    public IActionResult Retry()
    {
        Console.WriteLine("[Retryable] Throwing to test automatic retries");
        throw new InvalidOperationException("Simulated failure");
    }

    [HttpPost("recurring")]
    public IActionResult AddRecurring([FromBody] string cron)
    {
        Console.WriteLine($"Custom recurring at {DateTime.UtcNow:O} with cron {cron}");
        return Ok(new { status = "Recurring job set", cron });
    }

    [HttpDelete("recurring")]
    public IActionResult RemoveRecurring()
    {
        Console.WriteLine("Recurring job removed");
        return NoContent();
    }
}
