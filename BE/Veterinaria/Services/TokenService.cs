using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Veterinaria.Api.DTOs.Auth;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public class TokenService(IConfiguration configuration)
{
    public LoginResponse CrearToken(Usuario usuario, string rol)
    {
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Falta configurar 'Jwt:Issuer'.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Falta configurar 'Jwt:Audience'.");
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta configurar 'Jwt:Key' en User Secrets.");
        var keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("'Jwt:Key' debe contener al menos 32 bytes para firmar tokens HS256.");
        }

        var expiraEnUtc = DateTimeOffset.UtcNow.AddMinutes(60);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id),
            new Claim(ClaimTypes.Role, rol)
        };
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiraEnUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(jwt),
            ExpiraEnUtc = expiraEnUtc,
            Rol = rol
        };
    }
}
