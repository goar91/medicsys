using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        var db = services.GetRequiredService<AppDbContext>();

        await EnsureRoleAsync(roleManager, Roles.Professor);
        await EnsureRoleAsync(roleManager, Roles.Student);
        await EnsureRoleAsync(roleManager, Roles.Odontologo);
        await EnsureRoleAsync(roleManager, Roles.Admin);
        await EnsureRoleAsync(roleManager, Roles.Auditoria);

        await EnsureDefaultAdminAsync(userManager, config);
        await EnsureDefaultAuditorAsync(userManager, config);

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
                await userManager.AddToRoleAsync(odontologo, Roles.Odontologo);
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
            await SeedAccountingCategoriesAsync(db);
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

        await SeedAccountingCategoriesAsync(db);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }

    private static async Task EnsureDefaultAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        var adminEmail = config["Seed:DefaultAdminEmail"] ?? "admin@medicsys.com";
        var adminPassword = config["Seed:DefaultAdminPassword"] ?? "Admin123!";
        var adminName = config["Seed:DefaultAdminName"] ?? "Administrador MEDICSYS";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                FullName = adminName,
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(admin, adminPassword);
            if (!created.Succeeded)
            {
                return;
            }
        }

        var roles = await userManager.GetRolesAsync(admin);
        if (!roles.Contains(Roles.Admin))
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }

    private static async Task EnsureDefaultAuditorAsync(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        var auditorEmail = config["Seed:DefaultAuditorEmail"] ?? "auditoria@medicsys.com";
        var auditorPassword = config["Seed:DefaultAuditorPassword"] ?? "Auditoria123!";
        var auditorName = config["Seed:DefaultAuditorName"] ?? "Auditoria MEDICSYS";

        var auditor = await userManager.FindByEmailAsync(auditorEmail);
        if (auditor == null)
        {
            auditor = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = auditorEmail,
                Email = auditorEmail,
                FullName = auditorName,
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(auditor, auditorPassword);
            if (!created.Succeeded)
            {
                return;
            }
        }

        var roles = await userManager.GetRolesAsync(auditor);
        if (!roles.Contains(Roles.Auditoria))
        {
            await userManager.AddToRoleAsync(auditor, Roles.Auditoria);
        }
    }

    private static async Task SeedAccountingCategoriesAsync(AppDbContext db)
    {
        if (await db.AccountingCategories.AnyAsync())
        {
            return;
        }

        var categories = new List<AccountingCategory>
        {
            new() { Id = Guid.NewGuid(), Group = "Ingresos", Name = "Ventas estimadas", Type = AccountingEntryType.Income, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Ingresos", Name = "Menos (descuentos, errores, etc.)", Type = AccountingEntryType.Income, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Ingresos", Name = "Ingresos por servicios", Type = AccountingEntryType.Income, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Ingresos", Name = "Otros ingresos", Type = AccountingEntryType.Income, MonthlyBudget = 0 },

            new() { Id = Guid.NewGuid(), Group = "Gastos Generales de Inicio", Name = "Permisos Municipales", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Generales de Inicio", Name = "Gastos de Constitución", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Generales de Inicio", Name = "Muebles de Oficina", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Generales de Inicio", Name = "Pago de Prestamos Anteriores", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Generales de Inicio", Name = "Computador", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Generales de Inicio", Name = "Software (general)", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Generales de Inicio", Name = "Compra de Materias Primas o Mercadería", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },

            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Sueldos y Salarios", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Pagos Iess", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Pagos Sri", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Honorarios Profesionales", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Capacitaciones", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Transportes de Mercadería", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Cánones /Arrendamiento", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Primas de Seguros", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Suministros varios", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Administrativos", Name = "Otros", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },

            new() { Id = Guid.NewGuid(), Group = "Gastos Marketing y Ventas", Name = "Publicidad", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Marketing y Ventas", Name = "Desarrollo de Marca / Identidad", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Marketing y Ventas", Name = "Sitio web", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Marketing y Ventas", Name = "Material de marketing impreso", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Marketing y Ventas", Name = "Tarifas de anuncios", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Marketing y Ventas", Name = "Mercadeo por Internet", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Marketing y Ventas", Name = "Ferias de muestras", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Marketing y Ventas", Name = "Otros", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },

            new() { Id = Guid.NewGuid(), Group = "Gastos Financieros", Name = "Intereses Bancarios", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Financieros", Name = "Comisiones Bancarias", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Financieros", Name = "Maquinas para Tarjeta de Crédito", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Gastos Financieros", Name = "Cheques / Estado de Cuenta", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },

            new() { Id = Guid.NewGuid(), Group = "Otros", Name = "Invitados Especiales", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Otros", Name = "Fiesta de lanzamiento", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Otros", Name = "Otros", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },

            new() { Id = Guid.NewGuid(), Group = "Impuestos", Name = "Gastos de impuestos", Type = AccountingEntryType.Expense, MonthlyBudget = 0 },
            new() { Id = Guid.NewGuid(), Group = "Costos", Name = "Coste de bienes vendidos", Type = AccountingEntryType.Expense, MonthlyBudget = 0 }
        };

        db.AccountingCategories.AddRange(categories);
        await db.SaveChangesAsync();
    }
}
