using GourmetApi.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration cfg)
    {
        await db.Database.MigrateAsync();

        var email = (cfg["Seed:SuperAdminEmail"] ?? "").Trim().ToLowerInvariant();
        var pass = (cfg["Seed:SuperAdminPassword"] ?? "").Trim();

        // Si no configuraste variables, no seedea (pero NO rompe el arranque)
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            return;

        var exists = await db.AdminUsers.AnyAsync(x => x.Email.ToLower() == email);
        if (exists) return;

        var user = new AdminUser
        {
            Email = email,
            Enabled = true,
            Role = AdminRole.SuperAdmin,
            CompanyId = null,
            CreatedAt = DateTime.UtcNow
        };

        var hasher = new PasswordHasher<AdminUser>();
        user.PasswordHash = hasher.HashPassword(user, pass);

        db.AdminUsers.Add(user);
        await db.SaveChangesAsync();
    }
}