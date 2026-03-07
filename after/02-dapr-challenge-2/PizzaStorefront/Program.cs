using Dapr.Client;
using PizzaStorefront.Services;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers().AddDapr();
builder.Services.AddKeyedSingleton<HttpClient>("pizza-kitchen", (_, _) =>
    DaprClient.CreateInvokeHttpClient("pizza-kitchen"));
builder.Services.AddKeyedSingleton<HttpClient>("pizza-delivery", (_, _) =>
    DaprClient.CreateInvokeHttpClient("pizza-delivery"));
builder.Services.AddSingleton<IStorefrontService, StorefrontService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// app.UseCloudEvents();
app.MapControllers();
app.Run();
