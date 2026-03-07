using System.Net.Http.Json;
using Dapr.Workflow;
using Microsoft.Extensions.DependencyInjection;
using PizzaWorkflow.Models;

namespace PizzaWorkflow.Activities;

public class DeliveryActivity : WorkflowActivity<Order, Order>
{
    private readonly HttpClient _deliveryClient;
    private readonly ILogger<DeliveryActivity> _logger;

    public DeliveryActivity([FromKeyedServices("pizza-delivery")] HttpClient deliveryClient, ILogger<DeliveryActivity> logger)
    {
        _deliveryClient = deliveryClient;
        _logger = logger;
    }

    public override async Task<Order> RunAsync(WorkflowActivityContext context, Order order)
    {
        try
        {
            _logger.LogInformation("Starting delivery process for order {OrderId}", order.OrderId);

            var resp = await _deliveryClient.PostAsJsonAsync("/delivery", order);
            resp.EnsureSuccessStatusCode();
            var response = (await resp.Content.ReadFromJsonAsync<Order>())!;

            _logger.LogInformation("Order {OrderId} delivered with status {Status}",
                order.OrderId, response.Status);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering order {OrderId}", order.OrderId);
            throw;
        }
    }
}
