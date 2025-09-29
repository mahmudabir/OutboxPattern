using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.DataAccess;
using EventBusWithTickerQ.EventHandlers.Created;
using EventBusWithTickerQ.EventHandlers.Updated;
using EventBusWithTickerQ.Events;
using EventBusWithTickerQ.Infrastructure;
using EventBusWithTickerQ.Services;

using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using TickerQ;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Models.Ticker;

namespace EventBusWithTickerQ
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();

            var tickerQConnection = builder.Configuration.GetConnectionString("TickerQ");

            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(tickerQConnection));

            builder.Services.AddTickerQ(opt =>
            {
                // Set fallback time out to check for missed jobs and execute.
                opt.UpdateMissedJobCheckDelay(TimeSpan.FromMinutes(1));
                // Set name of instance, default is Environment.MachineName.
                opt.SetInstanceIdentifier("TickerQ");

                // Set your class that implements ITickerExceptionHandler.  
                opt.SetExceptionHandler<TickerExceptionHandler>();
                // Set the max thread concurrency for Ticker (default: Environment.ProcessorCount).
                opt.SetMaxConcurrency(maxConcurrency: Convert.ToInt32(builder.Configuration["TickerQ:MaxConcurrency"] ?? "100"));

                // Configure the EF Core�backed operational store for TickerQ metadata, locks, and state.
                opt.AddOperationalStore<ApplicationDbContext>(efOpt =>
                {
                    // Apply custom model configuration only during EF Core migrations
                    // (design-time). The runtime model remains unaffected.
                    efOpt.UseModelCustomizerForMigrations();

                    // On app start, cancel tickers left in Expired or InProgress (terminated) states
                    // to prevent duplicate re-execution after crashes or abrupt shutdowns.
                    efOpt.CancelMissedTickersOnAppStart();

                    // Defined cron-based functions are auto-seeded in the database by default.
                    // Example: [TickerFunction(..., "*/5 * * * *")]
                    // Use this to ignore them and keep seeds runtime-only.
                    efOpt.IgnoreSeedMemoryCronTickers();

                    // Seed initial tickers (time-based and cron-based).
                    efOpt.UseTickerSeeder(
                        async timeTicker =>
                        {
                            await timeTicker.AddAsync(new TimeTicker
                            {
                                Id = Guid.CreateVersion7(),
                                Function = "CleanupLogs",
                                ExecutionTime = DateTime.UtcNow.AddSeconds(5),
                            });
                        },
                        async cronTicker =>
                        {
                            await cronTicker.AddAsync(new CronTicker
                            {
                                Id = Guid.CreateVersion7(),
                                Expression = "0 0 * * *", // every day at 00:00 UTC
                                Function = "CleanupLogs"
                            });
                        });
                });

                // Dashboard configuration
                opt.AddDashboard(dbopt =>
                {
                    // Mount path for the dashboard UI (default: "/tickerq-dashboard").
                    dbopt.BasePath = "/tickerq";

                    // Allowed CORS origins for dashboard API (default: ["*"]).
                    dbopt.CorsOrigins = new[] { "https://arcenox.com" };

                    // Backend API domain (scheme/SSL prefix supported).
                    //dbopt.BackendDomain = "ssl:arcenox.com";

                    // Authentication
                    dbopt.EnableBuiltInAuth = true;  // Use TickerQ�s built-in auth (default).
                    dbopt.UseHostAuthentication = false; // Use host auth instead (off by default).
                    dbopt.RequiredRoles = new[] { "Admin", "Ops" };
                    dbopt.RequiredPolicies = new[] { "TickerQDashboardAccess" };

                    // Basic auth toggle (default: false).
                    //dbopt.EnableBasicAuth = true;

                    // Pipeline hooks
                    dbopt.PreDashboardMiddleware = app => { /* e.g., request logging */ };
                    dbopt.CustomMiddleware = app => { /* e.g., extra auth/rate limits */ };
                    dbopt.PostDashboardMiddleware = app => { /* e.g., metrics collection */ };
                });
            });

            builder.Services.AddScoped<MyBackgroundService>();
            builder.Services.AddScoped<MyFirstExample>();

            builder.Services.AddScoped<IEventBus, TickerQEventBus>();
            builder.Services.AddScoped<EventDispatchJob>();
            
            // Register handlers with both generic and non-generic interfaces
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreateEvent>, SendEmailOnOrderCreatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreateEvent>, UpdateReadModelOnOrderCreatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderUpdateEvent>, SendEmailOnOrderUpdatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderUpdateEvent>, UpdateReadModelOnOrderUpdatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler, SendEmailOnOrderCreatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler, UpdateReadModelOnOrderCreatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler, SendEmailOnOrderUpdatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler, UpdateReadModelOnOrderUpdatedHandler>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.EnableTryItOutByDefault();
                    c.DisplayRequestDuration();
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            RecurringJobs.Register();

            app.MapControllers();
            app.UseTickerQ();

            app.Run();
        }
    }
}
