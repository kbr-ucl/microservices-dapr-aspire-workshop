using PizzaStorefront.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers().AddDapr();
builder.Services.AddSingleton<IStorefrontService, StorefrontService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{

}

app.MapControllers();
app.Run();