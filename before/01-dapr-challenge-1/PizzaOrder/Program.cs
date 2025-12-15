using PizzaOrder.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IOrderStateService, OrderStateService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}

app.MapControllers();
app.Run();