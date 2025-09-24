using EventBusWithHangfire.Abstractions;
using EventBusWithHangfire.Events;
using EventBusWithHangfire.EventHandlers;
using EventBusWithHangfire.Infrastructure;
using Hangfire;
using Hangfire.MemoryStorage;

namespace EventBusWithHangfire
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();

            // Hangfire configuration: queues for events and maintenance, and dashboard authorization example
            builder.Services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UseMemoryStorage();
            });

            // Configure Hangfire server with multiple queues
            builder.Services.AddHangfireServer(options =>
            {
                options.Queues = ["default", "events", "maintenance"];
                options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
                options.WorkerCount = 8;//Math.Max(Environment.ProcessorCount, 2);
            });

            // Event Bus with Hangfire
            builder.Services.AddScoped<IEventBus, HangfireEventBus>();
            builder.Services.AddScoped<EventDispatchJob>();
            // Register event handlers for DI
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedEvent>, SendEmailOnOrderCreatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedEvent>, UpdateReadModelOnOrderCreatedHandler>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            // Hangfire Dashboard with custom authorization filter
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthorizationFilter()],
            });

            // Register recurring jobs at startup
            RecurringJobs.Register();

            app.MapControllers();

            app.Run();
        }
    }
}
