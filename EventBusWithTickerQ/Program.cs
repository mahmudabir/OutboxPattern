using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.Events;
using EventBusWithTickerQ.EventHandlers;
using EventBusWithTickerQ.Infrastructure;
using TickerQ;

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

            // Register TickerQ client (replace with actual implementation)
            builder.Services.AddSingleton<ITickerQClient, TickerQClient>();

            builder.Services.AddScoped<IEventBus, TickerQEventBus>();
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

            app.UseAuthorization();


            RecurringJobs.Register();
            app.MapControllers();

            app.Run();
        }
    }
}
