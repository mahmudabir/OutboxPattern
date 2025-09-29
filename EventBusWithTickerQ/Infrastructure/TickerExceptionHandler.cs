using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;

namespace EventBusWithTickerQ.Infrastructure
{
    public class TickerExceptionHandler(ILogger<TickerExceptionHandler> logger) : ITickerExceptionHandler
    {
        public Task HandleCanceledExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
        {
            logger.LogError(exception, "Canceled Exception in {TickerType} ticker {TickerId}", tickerType, tickerId);
            return Task.CompletedTask;
        }

        public Task HandleExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
        {
            logger.LogError(exception, "Exception in {TickerType} ticker {TickerId}", tickerType, tickerId);
            return Task.CompletedTask;
        }
    }
}
