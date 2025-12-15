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

        throw new NotImplementedException("TODO");
    }
}