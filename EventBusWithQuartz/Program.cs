using EventBusWithQuartz.Abstractions;
using EventBusWithQuartz.Events;
using EventBusWithQuartz.EventHandlers;
using EventBusWithQuartz.Infrastructure;
using Quartz;

namespace EventBusWithQuartz
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();

            // Quartz configuration
            builder.Services.AddQuartz(q =>
            {
                //q.UseMicrosoftDependencyInjectionJobFactory();
                q.UseSimpleTypeLoader();
                q.UseInMemoryStore(); // For demo; replace with persistent store in production
                q.UseDefaultThreadPool(tp => tp.MaxConcurrency = Math.Max(Environment.ProcessorCount, 4));
            });
            builder.Services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
                options.AwaitApplicationStarted = true;
            });

            builder.Services.AddScoped<IEventBus, QuartzEventBus>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedEvent>, SendEmailOnOrderCreatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedEvent>, UpdateReadModelOnOrderCreatedHandler>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            // Register recurring jobs after scheduler is started
            using (var scope = app.Services.CreateScope())
            {
                var schedulerFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
                var scheduler = await schedulerFactory.GetScheduler();
                await RecurringJobs.RegisterAsync(scheduler);
            }

            await app.RunAsync();
        }
    }
}
