using Microsoft.EntityFrameworkCore;
using OutboxPatternWithQuartz.Data;
using OutboxPatternWithQuartz.Services;
using OutboxPatternWithQuartz.EventBus;
using Quartz;

namespace OutboxPatternWithQuartz
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

            // EF Core InMemory
            builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("OutboxDbQuartz"));

            // Quartz
            builder.Services.AddQuartz(q =>
            {
                var jobKey = new JobKey("OutboxJob");
                q.AddJob<OutboxProcessor>(opts => opts.WithIdentity(jobKey));
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("OutboxJob-trigger")
                    .WithCronSchedule("0/15 * * * * ?")); // every 15 seconds
            });
            builder.Services.AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });

            // Event bus & handlers
            builder.Services.AddScoped<IEventBus, EventBus.EventBus>();
            builder.Services.AddScoped<IEventBusHandler<Events.OrderCreatedEvent>, OrderCreatedEventHandler>();
            builder.Services.AddScoped<IEventBusHandler<Events.OrderShippedEvent>, OrderShippedEventHandler>();
            builder.Services.AddScoped<IEventBusHandler<Events.MailSendEvent>, MailSendEventHandler>();
            builder.Services.AddScoped<IEventBusHandler<Events.InventoryUpdateEvent>, InventoryUpdateEventHandler>();

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
            app.MapControllers();

            app.Run();
        }
    }
}
