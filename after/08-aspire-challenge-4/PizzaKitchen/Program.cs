using PizzaKitchen.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers().AddDapr();
builder.Services.AddSingleton<ICookService, CookService>();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{

}

app.MapControllers();
app.Run();