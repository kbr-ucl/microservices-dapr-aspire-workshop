using Dapr.Workflow;
using PizzaWorkflow.Activities;
using PizzaWorkflow.Workflows;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// BUG: The following line is missing from the original code
builder.Services.AddControllers().AddDapr();

builder.Services.AddOpenApi();

// TODO: Register Dapr Workflow services


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();