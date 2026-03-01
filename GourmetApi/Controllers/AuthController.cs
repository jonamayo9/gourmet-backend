using GourmetApi.Data;
using GourmetApi.Dtos.Auth;
using GourmetApi.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GourmetApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly AppDbContext _db;

    public AuthController(IConfiguration cfg, AppDbContext db)
    {
        _cfg = cfg;
        _db = db;
    }

    private static bool VerifyPassword(AdminUser admin, string password)
    {
        var hash = admin.PasswordHash ?? "";

        // BCrypt
        if (hash.StartsWith("$2"))
            return BCrypt.Net.BCrypt.Verify(password, hash);

        // Identity PasswordHasher
        var hasher = new PasswordHasher<AdminUser>();
        var ok = hasher.VerifyHashedPassword(admin, hash, password);
        return ok != PasswordVerificationResult.Failed;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var slug = (req.CompanySlug ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
            return Unauthorized(new { message = "Credenciales inválidas" });

        // 1) Usuario por email (único global)
        var admin = await _db.AdminUsers
            .FirstOrDefaultAsync(a => a.Email.ToLower() == email);

        if (admin == null || !admin.Enabled)
            return Unauthorized(new { message = "Credenciales inválidas" });

        // 2) Si NO es SuperAdmin, validar empresa
        Company? company = null;

        if (admin.Role != AdminRole.SuperAdmin)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return Unauthorized(new { message = "Empresa requerida" });

            company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug && c.Enabled);

            if (company == null)
                return Unauthorized(new { message = "Empresa inválida" });

            if (admin.CompanyId != company.Id)
                return Unauthorized(new { message = "Credenciales inválidas" });
        }

        // 3) Password check (dual: BCrypt o Identity)
        if (!VerifyPassword(admin, req.Password))
            return Unauthorized(new { message = "Credenciales inválidas" });

        // 4) update last login
        admin.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // 5) JWT
        var jwt = _cfg.GetSection("Jwt");
        var key = jwt["Key"]!;
        var issuer = jwt["Issuer"]!;
        var audience = jwt["Audience"]!;
        var expMin = int.Parse(jwt["ExpiresMinutes"] ?? "480");

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expMin);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Role, admin.Role.ToString()),
        };

        if (admin.CompanyId.HasValue)
            claims.Add(new Claim("companyId", admin.CompanyId.Value.ToString()));

        if (company != null)
            claims.Add(new Claim("companySlug", company.Slug));

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponseDto
        {
            AccessToken = tokenString,
            ExpiresAtUtc = expiresAtUtc,
            ExpiresInMinutes = expMin,
            Role = admin.Role.ToString(),
            CompanyId = admin.CompanyId,
            CompanySlug = company?.Slug
        });
    }
}