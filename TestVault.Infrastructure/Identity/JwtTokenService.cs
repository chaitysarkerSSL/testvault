using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TestVault.Application.Interfaces;

namespace TestVault.Infrastructure.Identity;

/// <summary>
/// Creates and hashes tokens for the JWT access-token / refresh-token pair.
/// Reads Jwt:Secret/Issuer/Audience/AccessTokenMinutes/RefreshTokenDays from
/// configuration - see appsettings.json - following the same
/// inject-IConfiguration-directly pattern as SqlConnectionFactory/ClaudeAiAnalysisProvider.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AccessTokenResult CreateAccessToken(string userId, string userName, IEnumerable<string> roles)
    {
        var secret = GetRequiredSecret();
        var issuer = _configuration["Jwt:Issuer"] ?? "TestVault";
        var audience = _configuration["Jwt:Audience"] ?? "TestVault";
        var minutes = _configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 15;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.UniqueName, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        // "role" (the short, JWT-conventional claim name), not
        // ClaimTypes.Role's long XML-namespace URI - constructing a
        // JwtSecurityToken directly (below) does NOT apply
        // JwtSecurityTokenHandler's outbound claim-type mapping the way
        // CreateToken(SecurityTokenDescriptor) would, so ClaimTypes.Role
        // would end up as a literal, awkward-to-consume URI key in the
        // token payload. Program.cs's TokenValidationParameters.RoleClaimType
        // is set to match this exactly, so [Authorize(Roles = ...)] still
        // resolves it correctly server-side - this is what a JWT client
        // (this project's own React frontend included) actually expects
        // to find.
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        var days = _configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 7;

        // 256 bits of randomness, base64url-encoded (no padding) so it's
        // safe to send as a bare JSON string / query value with no escaping
        // surprises.
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var token = Base64UrlEncoder.Encode(randomBytes);

        return new RefreshTokenResult(token, DateTime.UtcNow.AddDays(days));
    }

    public string HashRefreshToken(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes);
    }

    private string GetRequiredSecret()
    {
        var secret = _configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            // Fails loudly and specifically rather than letting a missing
            // secret surface later as a confusing crypto exception deep in
            // JwtSecurityTokenHandler - see also the equivalent startup
            // check in Program.cs, which catches this before the app even
            // starts accepting requests.
            throw new InvalidOperationException(
                "Jwt:Secret is not configured (or is shorter than 32 characters). " +
                "Set it via an environment variable (Jwt__Secret) or User Secrets - never commit it to appsettings.json.");
        }

        return secret;
    }
}
