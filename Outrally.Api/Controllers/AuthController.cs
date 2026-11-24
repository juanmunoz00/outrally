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

public record RegisterRequest(string Email, string FirstName, string LastName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Email, string FirstName, string Role, bool MustChangePassword);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

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
            OrganizationId = 1,
            MustChangePassword = true
        };

        var tempPassword = GenerateTempPassword();
        var result = await _userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _emailService.SendWelcomeEmailAsync(req.Email, req.FirstName, tempPassword);
        return Ok(new { message = "Registered successfully. Check your email for your temporary password." });
    }    

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, req.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        var token = GenerateToken(user);
        return Ok(new AuthResponse(token, user.Email!, user.FirstName, user.Role.ToString(), user.MustChangePassword));        
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
        if (user == null) return Ok(); // avoid enumeration

        var tempPassword = GenerateTempPassword();
        await _userManager.RemovePasswordAsync(user);
        await _userManager.AddPasswordAsync(user, tempPassword);
        user.MustChangePassword = true;
        await _userManager.UpdateAsync(user);

        await _emailService.SendTempPasswordEmailAsync(req.Email, user.FirstName, tempPassword);
        return Ok();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.First().Description);

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        var token = GenerateToken(user);
        return Ok(new AuthResponse(token, user.Email!, user.FirstName, user.Role.ToString(), false));
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

    private static string GenerateTempPassword()
    {
        var words = new[] { "Trail", "Ridge", "Peak", "Summit", "Hike", "Canyon", "River", "Camp" };
        var word = words[Random.Shared.Next(words.Length)];
        var digits = Random.Shared.Next(1000, 9999);
        return $"{word}#{digits}";
    }

}
