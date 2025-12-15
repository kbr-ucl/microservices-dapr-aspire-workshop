using PizzaStorefront.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IStorefrontService, StorefrontService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}

// app.UseCloudEvents();
app.MapControllers();
app.Run();