using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Common.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EmergencyResponse.Infrastructure.Identity;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public Task<(string Token, DateTime ExpiresAtUtc)> GenerateTokenAsync(
        AuthenticationUserInfo user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(
            _options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name,
                $"{user.FirstName} {user.LastName}")
        };

        claims.AddRange(
            user.Roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SecretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenString =
            new JwtSecurityTokenHandler().WriteToken(token);

        return Task.FromResult(
            (tokenString, expiresAtUtc));
    }
}