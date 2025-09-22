namespace EventBusWithRxNet.Infrastructure
{
    public class EventBusOptions
    {
        public int RetryCount { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 500;
    }
}