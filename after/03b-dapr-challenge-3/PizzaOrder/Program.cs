using PizzaOrder.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers().AddDapr();
builder.Services.AddSingleton<IOrderStateService, OrderStateService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}
// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

// needed for Dapr pub/sub routing
app.MapSubscribeHandler();

app.MapControllers();
app.Run();