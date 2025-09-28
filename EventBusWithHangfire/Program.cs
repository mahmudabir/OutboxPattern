using EventBusWithHangfire.Abstractions;
using EventBusWithHangfire.Events;
using EventBusWithHangfire.EventHandlers;
using EventBusWithHangfire.Infrastructure;
using Hangfire;
using Hangfire.SqlServer;

namespace EventBusWithHangfire
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();

            // Connection string for LocalDB (remove stray '>')
            var hangfireConnection = builder.Configuration.GetConnectionString("Hangfire");

            // Hangfire with SQL Server storage (no MemoryStorage in production path)
            builder.Services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
                      {
                          //PrepareSchemaIfNecessary = true,
                          //QueuePollInterval = TimeSpan.Zero, // immediate fetch
                          //SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                          //CommandBatchMaxTimeout = TimeSpan.FromMinutes(1),
                          //UseRecommendedIsolationLevel = true,
                          //DisableGlobalLocks = true
                      });
            });

            // Hangfire server tuned for low latency on 'events'
            builder.Services.AddHangfireServer(options =>
            {
                options.Queues = new[] { "default", "events", "maintenance" };
                options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
                //options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
                //options.HeartbeatInterval = TimeSpan.FromSeconds(10);
                //options.ServerCheckInterval = TimeSpan.FromSeconds(2);
            });

            //// Boost minimum threads to reduce cold start waits
            //ThreadPool.GetMinThreads(out var workerMin, out var ioMin);
            //if (workerMin < 50)
            //{
            //    ThreadPool.SetMinThreads(50, ioMin);
            //}

            builder.Services.AddScoped<IEventBus, HangfireEventBus>();
            builder.Services.AddScoped<EventDispatchJob>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedEvent>, SendEmailOnOrderCreatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedEvent>, UpdateReadModelOnOrderCreatedHandler>();

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
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthorizationFilter()],
            });

            // Warm-up job to ensure workers spin up & DB schema created
            //BackgroundJob.Enqueue(() => Console.WriteLine($"Warm-up job at {DateTime.UtcNow:O}"));

            RecurringJobs.Register();
            app.MapControllers();
            app.Run();
        }
    }
}
