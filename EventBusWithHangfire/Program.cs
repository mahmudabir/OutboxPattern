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
                options.Queues = new[] { "default", "events", "maintenance" };
                options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);
            });

            // Event Bus with Hangfire
            builder.Services.AddScoped<IEventBus, HangfireEventBus>();
            builder.Services.AddScoped<EventDispatchJob>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            // Hangfire Dashboard (unrestricted in dev). For production, plug in auth filter.
            app.UseHangfireDashboard("/hangfire");

            // Register recurring jobs at startup
            RecurringJobs.Register();

            app.MapControllers();

            app.Run();
        }
    }
}
