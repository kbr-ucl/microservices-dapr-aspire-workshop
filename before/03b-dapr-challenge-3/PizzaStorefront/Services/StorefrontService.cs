using PizzaStorefront.Models;
using Dapr.Client;

namespace PizzaStorefront.Services;

public interface IStorefrontService
{
    Task<Order> ProcessOrderAsync(Order order);
}

public class StorefrontService : IStorefrontService
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<StorefrontService> _logger;

    public StorefrontService(DaprClient daprClient, ILogger<StorefrontService> logger)
    {
        _daprClient = daprClient;
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