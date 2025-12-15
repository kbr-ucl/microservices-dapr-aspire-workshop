using PizzaKitchen.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICookService, CookService>();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}

app.MapControllers();
app.Run();