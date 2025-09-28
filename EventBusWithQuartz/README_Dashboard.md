# Quartz.NET Dashboard

This project includes a comprehensive web dashboard for monitoring and managing Quartz.NET scheduled jobs.

## Features

### Dashboard Capabilities
- ?? **Scheduler Information**: View scheduler status, name, instance ID, and version
- ?? **Jobs Management**: List all jobs with their details, triggers, and status
- ?? **Triggers Overview**: Monitor trigger states, fire times, and schedules
- ?? **Executing Jobs**: See currently running jobs in real-time
- ??? **Job Control**: Pause, resume, trigger, and delete jobs directly from the UI
- ?? **Auto-refresh**: Dashboard automatically updates every 30 seconds
- ?? **Health Checks**: Built-in health check endpoint for monitoring

### API Endpoints

The dashboard is powered by REST API endpoints:

#### Scheduler Information
- `GET /api/QuartzDashboard/scheduler-info` - Get scheduler status and metadata

#### Jobs Management
- `GET /api/QuartzDashboard/jobs` - List all jobs with details
- `GET /api/QuartzDashboard/triggers` - List all triggers
- `GET /api/QuartzDashboard/executing-jobs` - Get currently executing jobs

#### Job Control
- `POST /api/QuartzDashboard/pause-job/{group}/{name}` - Pause a specific job
- `POST /api/QuartzDashboard/resume-job/{group}/{name}` - Resume a specific job
- `POST /api/QuartzDashboard/trigger-job/{group}/{name}` - Manually trigger a job
- `DELETE /api/QuartzDashboard/delete-job/{group}/{name}` - Delete a job

#### Health Monitoring
- `GET /health` - Health check endpoint

## Usage

### Accessing the Dashboard

1. **Start the application:**
   ```bash
   dotnet run
   ```

2. **Access the dashboard:**
   - Main dashboard: `https://localhost:5001/dashboard.html`
   - Root redirect: `https://localhost:5001/` (automatically redirects to dashboard)
   - Alternative URL: `https://localhost:5001/dashboard`

### Dashboard Interface

The dashboard provides several sections:

#### 1. Scheduler Information
Shows real-time status of the Quartz scheduler including:
- Scheduler name and instance ID
- Current status (Started/Stopped)
- Standby mode status
- Version information

#### 2. Jobs Section
Lists all registered jobs with:
- Job key (group.name format)
- Job type/class name
- Description
- Durability status
- Number of associated triggers
- Action buttons (Trigger, Pause, Resume, Delete)

#### 3. Triggers Section
Displays all triggers with:
- Trigger key
- Associated job key
- Current state (Normal, Paused, etc.)
- Next and previous fire times
- Priority level

#### 4. Executing Jobs
Shows currently running jobs with:
- Job and trigger keys
- Fire time
- Current run time in milliseconds
- Recovery status

### Manual Job Control

You can control jobs directly from the dashboard:

- **?? Trigger**: Execute a job immediately
- **?? Pause**: Temporarily stop a job from executing
- **?? Resume**: Resume a paused job
- **??? Delete**: Permanently remove a job (with confirmation)

### API Integration

The dashboard APIs can also be used programmatically:

```csharp
// Example: Trigger a job via HTTP
var client = new HttpClient();
await client.PostAsync("https://localhost:5001/api/QuartzDashboard/trigger-job/DEFAULT/MyJob", null);
```

```javascript
// Example: Get scheduler info via JavaScript
fetch('/api/QuartzDashboard/scheduler-info')
  .then(response => response.json())
  .then(data => console.log(data));
```

## Configuration

The dashboard uses the existing Quartz configuration from `Program.cs`. Key features:

- **Persistent Storage**: Uses SQL Server with Entity Framework
- **Health Checks**: Integrated health monitoring
- **CORS**: Enabled for development scenarios
- **Static Files**: Serves the dashboard HTML/CSS/JS

## Development

### Adding Custom Features

To extend the dashboard:

1. **Add new API endpoints** in `QuartzDashboardController.cs`
2. **Update the HTML/JavaScript** in `wwwroot/dashboard.html`
3. **Implement job listeners** for advanced features like execution history

### Example: Adding Job History

```csharp
// In QuartzDashboardController.cs
[HttpGet("job-executions/{group}/{name}")]
public async Task<IActionResult> GetJobExecutions(string group, string name)
{
    // Implement custom job execution logging
    // This would require a custom job listener
    return Ok(executions);
}
```

## Security Considerations

?? **Important**: This dashboard is designed for development and internal use. For production:

1. **Add Authentication**: Implement proper authentication/authorization
2. **Restrict CORS**: Configure CORS for specific domains only
3. **Use HTTPS**: Ensure all communications are encrypted
4. **Validate Input**: Add input validation for job operations
5. **Audit Logging**: Log all job management operations

## Troubleshooting

### Common Issues

1. **Dashboard not loading**: Ensure static files middleware is configured
2. **API errors**: Check that Quartz is properly initialized
3. **Connection issues**: Verify database connection string in `appsettings.json`
4. **CORS errors**: Ensure CORS is properly configured for your domain

### Debugging

Enable detailed logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Quartz": "Debug"
    }
  }
}
```

## Package Dependencies

The dashboard uses these NuGet packages:
- `Quartz` (3.15.0) - Core Quartz.NET functionality
- `Quartz.Extensions.Hosting` (3.15.0) - ASP.NET Core integration
- `Quartz.AspNetCore` (3.15.0) - Additional ASP.NET Core features
- `Quartz.Serialization.SystemTextJson` (3.15.0) - JSON serialization

## License

This dashboard implementation is part of the OutboxPattern project.