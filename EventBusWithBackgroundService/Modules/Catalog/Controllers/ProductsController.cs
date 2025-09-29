using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OutboxPattern.Infrastructure.EventBus;
using OutboxPattern.Modules.Catalog.Events;

namespace OutboxPattern.Modules.Catalog.Controllers;

[ApiController]
[Route("api/catalog/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IEventBus _bus;
    private readonly ILogger _logger;

    public ProductsController(IEventBus bus, ILogger<ProductsController> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        // Persist product in DB here (omitted). Then publish domain event
        var productId = Guid.CreateVersion7();
        //await Task.Delay(3000);
        _logger.LogInformation("[Product] Created Product {ProductId}", productId);
        
        await _bus.PublishAsync(new ProductCreated(productId, request.Name), cancellationToken);
        
        // Fire-and-forget: return immediately
        return Ok(new
        {
            isSuccess = true,
            message = "Product created",
            productId = productId,
            name = request.Name
        });
    }
}

public sealed class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
}
