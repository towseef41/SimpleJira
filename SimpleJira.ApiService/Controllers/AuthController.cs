using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace SimpleJira.ApiService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    // Simple dev token issuer. In production, replace with real user auth.
    [HttpPost("token")]
    [AllowAnonymous]
    public IActionResult IssueToken([FromBody] DevLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest("Username is required.");

        var issuer = _config["Jwt:Issuer"] ?? "simplejira";
        var audience = _config["Jwt:Audience"] ?? "simplejira-web";
        var key = _config["Jwt:Key"] ?? "dev-secret-change-me";
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim("name", request.Username)
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token)
        });
    }
}

public record DevLoginRequest(string Username);
