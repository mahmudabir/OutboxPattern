using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace EventBusWithHangfire.Infrastructure;

// Example dashboard authorization filter for production setups
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        // Plug your own auth logic here (e.g., cookie, bearer token, roles)
        return true; // Allow all for demo
    }
}
