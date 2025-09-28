using Microsoft.EntityFrameworkCore;
using OutboxPatternWithHangfire.Data;
using OutboxPatternWithHangfire.Services;
using OutboxPatternWithHangfire.EventBus;
using Hangfire;
using Hangfire.MemoryStorage;

namespace OutboxPatternWithHangfire
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

            // Add EF Core InMemory
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("OutboxDb"));

            // Add Hangfire (MemoryStorage)
            builder.Services.AddHangfire(config => config.UseMemoryStorage());
            builder.Services.AddHangfireServer();

            // Register OutboxProcessor and EventBus
            builder.Services.AddScoped<OutboxProcessor>();
            builder.Services.AddScoped<IEventBus, EventBus.EventBus>();

            // Register event handlers
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
                app.UseSwaggerUI(c =>
                {
                    c.EnableTryItOutByDefault();
                    c.DisplayRequestDuration();
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            // Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire");

            // Recurring job for outbox processing
            RecurringJob.AddOrUpdate<OutboxProcessor>("outbox-job", x => x.ProcessOutboxAsync(), "0/15 * * * * *");

            app.Run();
        }
    }
}
