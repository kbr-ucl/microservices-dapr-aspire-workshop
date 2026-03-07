using Microsoft.Extensions.DependencyInjection;
using PizzaStorefront.Models;

namespace PizzaStorefront.Services;

public interface IStorefrontService
{
    Task<Order> ProcessOrderAsync(Order order);
}

public class StorefrontService : IStorefrontService
{
    private readonly HttpClient _kitchenClient;
    private readonly HttpClient _deliveryClient;
    private readonly ILogger<StorefrontService> _logger;

    public StorefrontService(
        [FromKeyedServices("pizza-kitchen")] HttpClient kitchenClient,
        [FromKeyedServices("pizza-delivery")] HttpClient deliveryClient,
        ILogger<StorefrontService> logger)
    {
        _kitchenClient = kitchenClient;
        _deliveryClient = deliveryClient;
        _logger = logger;
    }

    public async Task<Order> ProcessOrderAsync(Order order)
    {
        var stages = new (string status, int duration)[]
        {
            ("validating", 1),
            ("processing", 2),
            ("confirmed", 1)
        };

        throw new NotImplementedException("TODO");
    }
}
