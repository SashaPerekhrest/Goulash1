using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Api.Options;
using Portfolio.Application.DTOs.Auth;
using Portfolio.Application.DTOs.Common;
using Portfolio.Application.Interfaces;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    PortfolioDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private const string InvalidCredentialsMessage = "Invalid login or password.";

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var login = request.Login.Trim();
        var user = await dbContext.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(adminUser => adminUser.Login == login, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ErrorResponse(InvalidCredentialsMessage));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.LifetimeMinutes);
        var accessToken = CreateAccessToken(user.Id, user.Login, expiresAt);

        return Ok(new LoginResponse(
            accessToken,
            expiresAt,
            new AuthUserDto(user.Id, user.Login)));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var login = User.FindFirstValue(ClaimTypes.Name);

        if (!Guid.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(login))
        {
            return Unauthorized(new ErrorResponse("Unauthorized."));
        }

        return Ok(new CurrentUserResponse(userId, login));
    }

    private string CreateAccessToken(Guid userId, string login, DateTime expiresAt)
    {
        var options = jwtOptions.Value;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, login),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, login)
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
