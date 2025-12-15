using PizzaKitchen.Models;

namespace PizzaKitchen.Services;

public interface ICookService
{
    Task<Order> CookPizzaAsync(Order order);
}

public class CookService : ICookService
{
    private readonly ILogger<CookService> _logger;

    public CookService(ILogger<CookService> logger)
    {
        _logger = logger;
    }

    public async Task<Order> CookPizzaAsync(Order order)
    {
        var stages = new (string status, int duration)[]
        {
            ("cooking_preparing_ingredients", 2),
            ("cooking_making_dough", 3),
            ("cooking_adding_toppings", 2),
            ("cooking_baking", 5),
            ("cooking_quality_check", 1)
        };

        try
        {
            foreach (var (status, duration) in stages)
            {
                order.Status = status;
                _logger.LogInformation("Order {OrderId} - {Status}", order.OrderId, status);

                await Task.Delay(TimeSpan.FromSeconds(duration));
            }

            order.Status = "cooked";
            _logger.LogInformation("Order {OrderId} - {Status}", order.OrderId, order.Status);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cooking order {OrderId}", order.OrderId);
            order.Status = "cooking_failed";
            order.Error = ex.Message;
            return order;
        }
    }
}