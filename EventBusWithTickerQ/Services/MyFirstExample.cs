using TickerQ.Dashboard.Controllers;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;

namespace EventBusWithTickerQ.Services
{
    public class MyFirstExample(ILogger<MyFirstExample> logger)
    {
        [TickerFunction("ExampleTicker")]
        public async Task ExampleTicker(TickerFunctionContext<string> tickerContext, CancellationToken cancellationToken)
        {
            logger.LogWarning(tickerContext.Request); // Output Hello
        }
    }
}
