using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Entities;
using TaskManagement.API.Helpers;

namespace TaskManagement.API.Data;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(AppDbContext context)
    {
        // 1. Ensure Roles exist
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "User" }
            );
            await context.SaveChangesAsync();
        }

        var adminEmail = "admin@taskmanagement.com";
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");

        if (adminUser == null)
        {
            // Create the admin if they don't exist
            adminUser = new User
            {
                FullName = "System Administrator",
                Email = adminEmail,
                PasswordHash = PasswordHasher.Hash("Admin@123"),
                RoleId = adminRole.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
        else
        {
            // FORCE UPDATE: Ensure the password is always exactly "Admin@123"
            adminUser.PasswordHash = PasswordHasher.Hash("Admin@123");
            adminUser.RoleId = adminRole.Id; // Ensure they are Admin
            await context.SaveChangesAsync();
        }
    }
}