namespace Veterinaria.Api.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiraEnUtc { get; set; }
    public string Rol { get; set; } = string.Empty;
}
