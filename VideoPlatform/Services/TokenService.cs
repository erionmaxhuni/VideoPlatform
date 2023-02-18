using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VideoPlatform.Models;

namespace VideoPlatform.Services;

public class TokenService
{
    private readonly string key;
    private readonly IConfiguration _Configruation;
    public TokenService(IConfiguration Configuration)
    {
        _Configruation = Configuration;
        key = _Configruation.GetValue<string>("JWTKey");
    }

    public TokenResponse GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            signingCredentials: credentials,
            issuer: "Elefanti-Video",
            audience: "Elefanti-Video",
            claims: new[]
            {
                new Claim("Id", user.Id.ToString()),
                new Claim("Name", user.Name),
                new Claim("Surname", user.Surname),
                new Claim("Username", user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            },
            expires: DateTime.UtcNow.AddHours(6));

        var jwtHandler = new JwtSecurityTokenHandler();
        return new() { Token = jwtHandler.WriteToken(jwt) };
    }

    public bool ValidateToken(string authToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = "Elefanti-Video",
            ValidAudience = "Elefanti-Video",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)) // The same key as the one that generate the token
        };
        try
        {
            var tokenInVerification = tokenHandler.ValidateToken(authToken, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase);

                if (result == false) return false;
            }
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public JwtPayload GetTokenPayload(string authHeader)
    {
        var parts = authHeader.Split("Bearer ");
        var authToken = parts[1];
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(authToken);

        return token.Payload;
    }
}

