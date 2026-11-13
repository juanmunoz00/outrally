using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Outrally.Infrastructure;
using System.Security.Cryptography;
using Outrally.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Outrally.Api.Controllers;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Email, string FirstName, string Role);
public record ForgotPasswordRequest(string Email, string AppBaseUrl);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config,
        AppDbContext db, IEmailService emailService)
    {
        _userManager = userManager;
        _config = config;
        _db = db;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            OrganizationId = 1
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, req.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        var token = GenerateToken(user);
        return Ok(new AuthResponse(token, user.Email!, user.FirstName, user.Role.ToString()));
    }

    private string GenerateToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("firstName", user.FirstName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

[HttpPost("forgot-password")]
[AllowAnonymous]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
{
    var user = await _userManager.FindByEmailAsync(req.Email);
    // Always return OK to avoid email enumeration
    if (user == null) return Ok();

    // Generate token
    var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    _db.PasswordResetTokens.Add(new PasswordResetToken
    {
        UserId = user.Id,
        TokenHash = hash,
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        Used = false
    });
    await _db.SaveChangesAsync();

    var resetLink = $"{req.AppBaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(req.Email)}";
    await _emailService.SendPasswordResetAsync(req.Email, resetLink);

    return Ok();
}

[HttpPost("reset-password")]
[AllowAnonymous]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
{
    var user = await _userManager.FindByEmailAsync(req.Email);
    if (user == null) return BadRequest("Invalid request.");

    var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(req.Token)));

    var record = await _db.PasswordResetTokens
        .Where(t => t.UserId == user.Id && t.TokenHash == hash && !t.Used && t.ExpiresAt > DateTime.UtcNow)
        .FirstOrDefaultAsync();

    if (record == null) return BadRequest("Token is invalid or expired.");

    var result = await _userManager.RemovePasswordAsync(user);
    if (!result.Succeeded) return BadRequest("Failed to reset password.");

    result = await _userManager.AddPasswordAsync(user, req.NewPassword);
    if (!result.Succeeded) return BadRequest(result.Errors.First().Description);

    record.Used = true;
    await _db.SaveChangesAsync();

    return Ok();
}

}
