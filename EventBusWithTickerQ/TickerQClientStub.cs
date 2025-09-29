namespace TickerQ;

public interface ITickerQClient
{
    void Enqueue<TJob>(Action<TJob> jobAction);
}

public class TickerQClient : ITickerQClient
{
    public void Enqueue<TJob>(Action<TJob> jobAction)
    {
        // Simulate job enqueue (replace with real implementation)
        // In a real scenario, this would enqueue the job to TickerQ
    }
}
