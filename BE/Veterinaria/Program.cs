using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Veterinaria")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:Veterinaria'. Configure user-secrets para el entorno local.");

builder.Services.AddDbContext<VeterinariaDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
