using Dapr.Client;
using Dapr.Workflow;
using PizzaWorkflow.Activities;
using PizzaWorkflow.Workflows;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers().AddDapr();
builder.Services.AddKeyedSingleton<HttpClient>("pizza-kitchen", (_, _) =>
    DaprClient.CreateInvokeHttpClient("pizza-kitchen"));
builder.Services.AddKeyedSingleton<HttpClient>("pizza-delivery", (_, _) =>
    DaprClient.CreateInvokeHttpClient("pizza-delivery"));
builder.Services.AddKeyedSingleton<HttpClient>("pizza-storefront", (_, _) =>
    DaprClient.CreateInvokeHttpClient("pizza-storefront"));

builder.Services.AddOpenApi();


builder.Services.AddDaprWorkflow(options =>
{
    // Register workflows
    options.RegisterWorkflow<PizzaOrderingWorkflow>();

    // Register activities
    options.RegisterActivity<StorefrontActivity>();
    options.RegisterActivity<CookingActivity>();
    options.RegisterActivity<ValidationActivity>();
    options.RegisterActivity<DeliveryActivity>();
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();