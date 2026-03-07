using System.Net.Http.Json;
using Dapr.Workflow;
using Microsoft.Extensions.DependencyInjection;
using PizzaWorkflow.Models;

namespace PizzaWorkflow.Activities;

public class CookingActivity : WorkflowActivity<Order, Order>
{
    private readonly HttpClient _kitchenClient;
    private readonly ILogger<CookingActivity> _logger;

    public CookingActivity([FromKeyedServices("pizza-kitchen")] HttpClient kitchenClient, ILogger<CookingActivity> logger)
    {
        _kitchenClient = kitchenClient;
        _logger = logger;
    }

    public override async Task<Order> RunAsync(WorkflowActivityContext context, Order order)
    {
        try
        {
            _logger.LogInformation("Starting cooking process for order {OrderId}", order.OrderId);

            var resp = await _kitchenClient.PostAsJsonAsync("/cook", order);
            resp.EnsureSuccessStatusCode();
            var response = (await resp.Content.ReadFromJsonAsync<Order>())!;

            _logger.LogInformation("Order {OrderId} cooked with status {Status}",
                order.OrderId, response.Status);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cooking order {OrderId}", order.OrderId);
            throw;
        }
    }
}
