using StrongTypes.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddStrongTypes();

var app = builder.Build();

app.MapControllers();

app.Run();
