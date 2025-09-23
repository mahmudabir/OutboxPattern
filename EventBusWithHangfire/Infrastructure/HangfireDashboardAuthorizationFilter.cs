using Hangfire.Annotations;
using Hangfire.Dashboard;
using System.Text;

namespace EventBusWithHangfire.Infrastructure;

// Example dashboard authorization filter for production setups
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var realm = $"Basic realm=\"{httpContext.Request.Host.Value}\"";

        string basicHeader = httpContext.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(basicHeader))
        {
            httpContext.Response.StatusCode = 401;
            httpContext.Response.Headers.WWWAuthenticate = realm;
            return false;
        }

        if (basicHeader.Contains(' '))
        {
            basicHeader = basicHeader.Split(' ')[1];
        }
        var decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(basicHeader));

        // splitting decodeAuthToken using ':'
        var splitText = decodedString.Split([':']);
        var isValidBasicHeader = splitText[0] == "sa" && splitText[1] == "sa";

        if (!isValidBasicHeader)
        {
            httpContext.Response.StatusCode = 401;
            httpContext.Response.Headers.WWWAuthenticate = realm;
            return false;
        }

        return true;
    }
}
