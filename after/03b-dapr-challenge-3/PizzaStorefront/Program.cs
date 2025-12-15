using PizzaStorefront.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers().AddDapr();
builder.Services.AddSingleton<IStorefrontService, StorefrontService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

app.MapControllers();
app.Run();