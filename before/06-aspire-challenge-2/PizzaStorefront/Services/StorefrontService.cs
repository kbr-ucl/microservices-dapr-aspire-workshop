using System.Net.Http.Json;
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

        try
        {
            foreach (var (status, duration) in stages)
            {
                order.Status = status;
                _logger.LogInformation("Order {OrderId} - {Status}", order.OrderId, status);

                await Task.Delay(TimeSpan.FromSeconds(duration));
            }

            _logger.LogInformation("Starting cooking process for order {OrderId}", order.OrderId);

            var resp = await _kitchenClient.PostAsJsonAsync("/cook", order);
            resp.EnsureSuccessStatusCode();
            var response = (await resp.Content.ReadFromJsonAsync<Order>())!;

            _logger.LogInformation("Order {OrderId} cooked with status {Status}",
                order.OrderId, response.Status);

            _logger.LogInformation("Starting delivery process for order {OrderId}", order.OrderId);

            resp = await _deliveryClient.PostAsJsonAsync("/delivery", response);
            resp.EnsureSuccessStatusCode();
            response = (await resp.Content.ReadFromJsonAsync<Order>())!;

            _logger.LogInformation("Order {OrderId} delivered with status {Status}",
                order.OrderId, response.Status);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order {OrderId}", order.OrderId);
            order.Status = "failed";
            order.Error = ex.Message;

            return order;
        }
    }
}
