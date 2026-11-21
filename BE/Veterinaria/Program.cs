using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Veterinaria")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:Veterinaria'. Configure user-secrets para el entorno local.");

builder.Services.AddDbContext<VeterinariaDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddIdentity<Usuario, IdentityRole>()
    .AddEntityFrameworkStores<VeterinariaDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<InicializadorIdentity>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var inicializador = scope.ServiceProvider.GetRequiredService<InicializadorIdentity>();
    await inicializador.InicializarAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
