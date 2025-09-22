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

            // Hangfire configuration (Memory storage for demo; replace with persistent storage in production)
            builder.Services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UseMemoryStorage();
            });
            builder.Services.AddHangfireServer();

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

            // Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire");

            app.MapControllers();

            app.Run();
        }
    }
}
