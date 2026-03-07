using System.Net.Http.Json;
using Dapr.Workflow;
using Microsoft.Extensions.DependencyInjection;
using PizzaWorkflow.Models;

namespace PizzaWorkflow.Activities;

public class StorefrontActivity : WorkflowActivity<Order, Order>
{
    private readonly HttpClient _storefrontClient;
    private readonly ILogger<StorefrontActivity> _logger;

    public StorefrontActivity([FromKeyedServices("pizza-storefront")] HttpClient storefrontClient, ILogger<StorefrontActivity> logger)
    {
        _storefrontClient = storefrontClient;
        _logger = logger;
    }

    public override async Task<Order> RunAsync(WorkflowActivityContext context, Order order)
    {
        try
        {
            _logger.LogInformation("Starting ordering process for order {OrderId}", order.OrderId);

            var resp = await _storefrontClient.PostAsJsonAsync("/storefront/order", order);
            resp.EnsureSuccessStatusCode();
            var response = (await resp.Content.ReadFromJsonAsync<Order>())!;

            _logger.LogInformation("Order {OrderId} processed with status {Status}",
                order.OrderId, response.Status);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order {OrderId}", order.OrderId);
            throw;
        }
    }
}
