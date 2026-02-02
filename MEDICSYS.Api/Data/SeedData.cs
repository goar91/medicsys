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
        await EnsureRoleAsync(roleManager, "Odontologo");

        // Crear usuario Odontólogo
        var odontologoEmail = "odontologo@medicsys.com";
        var odontologoPassword = "Odontologo123!";
        var odontologoExisting = await userManager.FindByEmailAsync(odontologoEmail);
        
        if (odontologoExisting == null)
        {
            var odontologo = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = odontologoEmail,
                Email = odontologoEmail,
                FullName = "Dr. Carlos Mendoza",
                EmailConfirmed = true
            };

            var createdOdontologo = await userManager.CreateAsync(odontologo, odontologoPassword);
            if (createdOdontologo.Succeeded)
            {
                await userManager.AddToRoleAsync(odontologo, "Odontologo");
            }
        }

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
