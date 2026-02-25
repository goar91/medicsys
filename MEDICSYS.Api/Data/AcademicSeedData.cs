using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Academico;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

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

        await SeedSupervisionAssignmentsAsync(db, profesor.Id, students);
        await SeedCacesCriteriaAsync(db, profesor.Id);
        await SeedRetentionPoliciesAsync(db, profesor.Id);

        Console.WriteLine("✅ Seed Académico: usuario profesor y estudiantes verificados");
    }

    private static async Task SeedCacesCriteriaAsync(AcademicDbContext db, Guid defaultUserId)
    {
        if (await db.AcademicAccreditationCriteria.AnyAsync())
        {
            return;
        }

        var now = DateTimeHelper.Now();
        var criteria = new List<AcademicAccreditationCriterion>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = "CACES-RA-01",
                Name = "Resultado de aprendizaje clínico",
                Dimension = "Resultados de Aprendizaje",
                Description = "Porcentaje de historias aprobadas en primera revisión.",
                TargetValue = 85m,
                CurrentValue = 72m,
                Status = AccreditationCriterionStatus.InProgress,
                CreatedByUserId = defaultUserId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "CACES-PE-02",
                Name = "Prácticas preprofesionales supervisadas",
                Dimension = "Entorno de Aprendizaje",
                Description = "Cobertura de estudiantes con prácticas registradas y supervisadas.",
                TargetValue = 95m,
                CurrentValue = 88m,
                Status = AccreditationCriterionStatus.InProgress,
                CreatedByUserId = defaultUserId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "CACES-GC-03",
                Name = "Gestión de mejora continua",
                Dimension = "Gestión de la Calidad",
                Description = "Cumplimiento de acciones del plan de mejoramiento de carrera.",
                TargetValue = 90m,
                CurrentValue = 60m,
                Status = AccreditationCriterionStatus.NeedsImprovement,
                CreatedByUserId = defaultUserId,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        db.AcademicAccreditationCriteria.AddRange(criteria);
        await db.SaveChangesAsync();
    }

    private static async Task SeedRetentionPoliciesAsync(AcademicDbContext db, Guid defaultUserId)
    {
        if (await db.AcademicDataRetentionPolicies.AnyAsync())
        {
            return;
        }

        var now = DateTimeHelper.Now();
        var policies = new List<AcademicDataRetentionPolicy>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DataCategory = "HistoriasClinicasAcademicas",
                RetentionMonths = 120,
                AutoDelete = false,
                IsActive = true,
                ConfiguredByUserId = defaultUserId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                DataCategory = "AuditTrailAcademico",
                RetentionMonths = 60,
                AutoDelete = false,
                IsActive = true,
                ConfiguredByUserId = defaultUserId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                DataCategory = "ConsentimientosLOPDP",
                RetentionMonths = 120,
                AutoDelete = false,
                IsActive = true,
                ConfiguredByUserId = defaultUserId,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        db.AcademicDataRetentionPolicies.AddRange(policies);
        await db.SaveChangesAsync();
    }

    private static async Task SeedSupervisionAssignmentsAsync(
        AcademicDbContext db,
        Guid professorId,
        IReadOnlyCollection<ApplicationUser> students)
    {
        var studentIds = students.Select(s => s.Id).Where(id => id != Guid.Empty).Distinct().ToList();
        if (studentIds.Count == 0)
        {
            return;
        }

        var existingStudentIds = await db.AcademicSupervisionAssignments
            .AsNoTracking()
            .Where(a => a.IsActive && a.ProfessorId == professorId && studentIds.Contains(a.StudentId))
            .Select(a => a.StudentId)
            .Distinct()
            .ToListAsync();

        var now = DateTimeHelper.Now();
        var newAssignments = studentIds
            .Where(studentId => !existingStudentIds.Contains(studentId))
            .Select(studentId => new AcademicSupervisionAssignment
            {
                Id = Guid.NewGuid(),
                ProfessorId = professorId,
                StudentId = studentId,
                PatientId = null,
                AssignedByUserId = professorId,
                IsActive = true,
                Notes = "Asignación académica inicial",
                AssignedAt = now,
                UpdatedAt = now
            })
            .ToList();

        if (newAssignments.Count == 0)
        {
            return;
        }

        db.AcademicSupervisionAssignments.AddRange(newAssignments);
        await db.SaveChangesAsync();
    }
}
