using PizzaOrder.Services;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddControllers().AddDapr();
builder.Services.AddSingleton<IOrderStateService, OrderStateService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

// Needed for Programmatic Dapr pub/sub routing
app.MapSubscribeHandler();

app.MapControllers();
app.Run();