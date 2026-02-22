using Microsoft.AspNetCore.Identity;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Data;

public static class AcademicSeedData
{
    public static async Task SeedAsync(AcademicDbContext db, UserManager<ApplicationUser> userManager)
    {
        // Crear usuario Profesor si no existe
        var profesor = await userManager.FindByEmailAsync("profesor@medicsys.com");
        if (profesor == null)
        {
            profesor = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "profesor@medicsys.com",
                Email = "profesor@medicsys.com",
                EmailConfirmed = true,
                FullName = "Dr. Fernando Sánchez"
            };
            await userManager.CreateAsync(profesor, "Profesor123!");
            await userManager.AddToRoleAsync(profesor, Roles.Professor);
        }

        // Crear 3 usuarios Estudiantes si no existen
        var students = new List<ApplicationUser>();
        for (int i = 1; i <= 3; i++)
        {
            var email = $"estudiante{i}@medicsys.com";
            var student = await userManager.FindByEmailAsync(email);
            if (student == null)
            {
                student = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = $"Estudiante {i}",
                    UniversityId = $"EST{i:000}"
                };
                await userManager.CreateAsync(student, "Estudiante123!");
                await userManager.AddToRoleAsync(student, Roles.Student);
            }
            students.Add(student);
        }

        // Los datos de pacientes, historias clínicas, citas, etc.
        // se gestionan únicamente desde la base de datos.
        // No se precargan datos ficticios desde el seed.

        Console.WriteLine("✅ Seed Académico: usuario profesor y estudiantes verificados");
    }
}
