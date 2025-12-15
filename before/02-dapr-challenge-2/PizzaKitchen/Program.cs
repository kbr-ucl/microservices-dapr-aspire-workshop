using PizzaKitchen.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ICookService, CookService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.MapControllers();
app.Run();