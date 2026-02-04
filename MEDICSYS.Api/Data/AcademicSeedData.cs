using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Academico;
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

        // Verificar si ya hay datos
        if (db.AcademicAppointments.Any()) return;

        // Crear citas académicas
        var appointments = new List<AcademicAppointment>();
        foreach (var student in students)
        {
            // Cita pasada (completada)
            appointments.Add(new()
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                ProfessorId = profesor.Id,
                PatientName = "Paciente de Práctica 1",
                Reason = "Consulta general - Práctica",
                StartAt = DateTime.UtcNow.AddDays(-7),
                EndAt = DateTime.UtcNow.AddDays(-7).AddHours(2),
                Status = AppointmentStatus.Completed,
                Notes = "Práctica supervisada completada",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            });

            // Cita futura (confirmada)
            appointments.Add(new()
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                ProfessorId = profesor.Id,
                PatientName = "Paciente de Práctica 2",
                Reason = "Limpieza dental - Práctica",
                StartAt = DateTime.UtcNow.AddDays(3),
                EndAt = DateTime.UtcNow.AddDays(3).AddHours(2),
                Status = AppointmentStatus.Confirmed,
                Notes = "Material preparado",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        db.AcademicAppointments.AddRange(appointments);
        await db.SaveChangesAsync();

        // Crear recordatorios
        var reminders = new List<AcademicReminder>();
        foreach (var appointment in appointments.Where(a => a.Status == AppointmentStatus.Confirmed))
        {
            var student = students.First(s => s.Id == appointment.StudentId);
            
            reminders.Add(new()
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                Message = $"Recordatorio: Práctica de {appointment.Reason}",
                Channel = "Email",
                Status = "Pending",
                Target = student.Email!,
                ScheduledAt = appointment.StartAt.AddHours(-24),
                CreatedAt = DateTime.UtcNow
            });

            reminders.Add(new()
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                Message = $"Recordatorio: Supervisión de práctica - {student.FullName}",
                Channel = "Email",
                Status = "Pending",
                Target = profesor.Email!,
                ScheduledAt = appointment.StartAt.AddHours(-2),
                CreatedAt = DateTime.UtcNow
            });
        }

        db.AcademicReminders.AddRange(reminders);
        await db.SaveChangesAsync();

        // Crear historias clínicas académicas
        var histories = new List<AcademicClinicalHistory>();
        int patientNum = 1;
        
        foreach (var student in students)
        {
            // Historia aprobada
            var approvedData = new JsonObject
            {
                ["personal"] = new JsonObject
                {
                    ["firstName"] = $"Paciente",
                    ["lastName"] = $"Académico {patientNum}",
                    ["idNumber"] = $"17000000{patientNum:00}",
                    ["dateOfBirth"] = "1995-06-15",
                    ["gender"] = "M",
                    ["phone"] = $"099000000{patientNum}"
                },
                ["dentalHistory"] = new JsonObject
                {
                    ["lastVisit"] = "2025-12-01",
                    ["brushingFrequency"] = "2 veces al día",
                    ["usesFloss"] = true
                },
                ["diagnosis"] = "Gingivitis leve",
                ["treatment"] = "Limpieza profunda y educación en higiene oral",
                ["odontogram"] = new JsonObject
                {
                    ["tooth18"] = new JsonObject { ["status"] = "sano" },
                    ["tooth11"] = new JsonObject { ["status"] = "obturado" }
                }
            };

            histories.Add(new()
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                ReviewedByProfessorId = profesor.Id,
                Data = approvedData,
                Status = ClinicalHistoryStatus.Approved,
                ProfessorComments = "Excelente diagnóstico y plan de tratamiento. Aprobado.",
                ReviewedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            });

            patientNum++;

            // Historia en draft (pendiente de revisión)
            var draftData = new JsonObject
            {
                ["personal"] = new JsonObject
                {
                    ["firstName"] = $"Paciente",
                    ["lastName"] = $"Académico {patientNum}",
                    ["idNumber"] = $"17000000{patientNum:00}",
                    ["dateOfBirth"] = "1988-03-20",
                    ["gender"] = "F",
                    ["phone"] = $"099000000{patientNum}"
                },
                ["diagnosis"] = "En proceso de evaluación",
                ["treatment"] = "Pendiente de aprobación del profesor"
            };

            histories.Add(new()
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                Data = draftData,
                Status = ClinicalHistoryStatus.Draft,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });

            patientNum++;
        }

        db.AcademicClinicalHistories.AddRange(histories);
        await db.SaveChangesAsync();

        Console.WriteLine("✅ Base de datos Académica poblada con datos de prueba");
    }
}
