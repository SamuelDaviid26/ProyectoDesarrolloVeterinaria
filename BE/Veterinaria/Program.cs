using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

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
builder.Services.AddScoped<DuenosService>();
builder.Services.AddScoped<EspeciesService>();
builder.Services.AddScoped<RazasService>();
builder.Services.AddScoped<CatalogoClinicoService>();
builder.Services.AddScoped<VeterinariosService>();
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddScoped<MascotasService>();
builder.Services.AddScoped<ExpedientesService>();

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Falta configurar 'Jwt:Issuer'.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Falta configurar 'Jwt:Audience'.");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar 'Jwt:Key' en User Secrets.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("'Jwt:Key' debe contener al menos 32 bytes para firmar tokens HS256.");
}

builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    context.Fail("El token no contiene la identidad del usuario.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Usuario>>();
                var usuario = await userManager.FindByIdAsync(userId);
                if (usuario is null || usuario.Bloqueado)
                {
                    context.Fail("La cuenta no está disponible.");
                    return;
                }

                var rolesActuales = await userManager.GetRolesAsync(usuario);
                var rolesToken = context.Principal?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct()
                    ?? Enumerable.Empty<string>();
                if (rolesToken.Any(rol => !rolesActuales.Contains(rol, StringComparer.Ordinal)))
                    context.Fail("Los permisos del token ya no están vigentes.");
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

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
