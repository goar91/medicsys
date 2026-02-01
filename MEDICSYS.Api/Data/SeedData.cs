using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        await EnsureRoleAsync(roleManager, Roles.Professor);
        await EnsureRoleAsync(roleManager, Roles.Student);

        var email = config["Seed:DefaultProfessorEmail"];
        var password = config["Seed:DefaultProfessorPassword"];
        var fullName = config["Seed:DefaultProfessorName"] ?? "Profesor Admin";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            return;
        }

        var professor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var created = await userManager.CreateAsync(professor, password);
        if (created.Succeeded)
        {
            await userManager.AddToRoleAsync(professor, Roles.Professor);
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }
}
