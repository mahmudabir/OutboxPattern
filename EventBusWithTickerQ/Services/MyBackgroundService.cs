using TickerQ.Utilities.Base;

namespace EventBusWithTickerQ.Services
{
    public class MyBackgroundService(ILogger<MyBackgroundService> logger)
    {
        [TickerFunction("ExampleMethod", "%CronTicker:EveryMinute%")]
        public void ExampleMethod()
        {
            logger.LogInformation("[TickerFunction] Running ExampleMethod");
        }

        [TickerFunction("DeactivateStaleUsers", "0 0 * * 0")]
        public void DeactivateStaleUsersAsync()
        {
            logger.LogInformation("[TickerFunction] Running DeactivateStaleUsers");
        }

        [TickerFunction("CleanUpUserSessions", "0 */2 * * *")]
        public void CleanUpUserSessions()
        {
            logger.LogInformation("[TickerFunction] Running CleanUpUserSessions");
        }
    }
}
