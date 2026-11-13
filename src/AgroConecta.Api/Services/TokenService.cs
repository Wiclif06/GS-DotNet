using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroConecta.Api.Domain.Entities;
using AgroConecta.Api.DTOs.Responses;
using Microsoft.IdentityModel.Tokens;

namespace AgroConecta.Api.Services;
public class TokenService(IConfiguration configuration)
{
    public TokenResponse Create(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(60);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"],
            new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role) },
            expires: expires,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expires,
            new UserSummary(user.Id, user.Name, user.Email, user.Role));
    }
}
