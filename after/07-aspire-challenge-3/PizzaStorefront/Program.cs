using Dapr.Client;
using PizzaStorefront.Services;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();
builder.Services.AddKeyedSingleton<HttpClient>("pizza-kitchen", (_, _) =>
    DaprClient.CreateInvokeHttpClient("pizza-kitchen"));
builder.Services.AddKeyedSingleton<HttpClient>("pizza-delivery", (_, _) =>
    DaprClient.CreateInvokeHttpClient("pizza-delivery"));
builder.Services.AddSingleton<IStorefrontService, StorefrontService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

app.MapControllers();
app.Run();