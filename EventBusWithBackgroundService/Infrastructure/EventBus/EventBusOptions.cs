namespace OutboxPattern.Infrastructure.EventBus;

public sealed class EventBusOptions
{
    // Number of retries after the initial attempt (total attempts = 1 + HandlerMaxRetries)
    public int HandlerMaxRetries { get; set; } = 3;

    // Base delay for exponential backoff
    public TimeSpan HandlerBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    // Maximum delay cap for backoff
    public TimeSpan HandlerMaxDelay { get; set; } = TimeSpan.FromSeconds(10);

    // +/- percentage jitter applied to delay (e.g., 0.25 = ±25%)
    public double JitterFactor { get; set; } = 0.25;
}
