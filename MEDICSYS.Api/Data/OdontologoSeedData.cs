using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Data;

public static class OdontologoSeedData
{
    public static async Task SeedAsync(OdontologoDbContext odontologoDb, UserManager<ApplicationUser> userManager)
    {
        // Crear usuario Odontólogo si no existe
        var odontologo = await userManager.FindByEmailAsync("odontologo@medicsys.com");
        if (odontologo == null)
        {
            odontologo = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "odontologo@medicsys.com",
                Email = "odontologo@medicsys.com",
                EmailConfirmed = true,
                FullName = "Dr. Carlos Rodríguez"
            };
            await userManager.CreateAsync(odontologo, "Odontologo123!");
            await userManager.AddToRoleAsync(odontologo, Roles.Odontologo);
        }

        // Poblar categorías contables en la base de datos de odontología
        await SeedAccountingCategoriesAsync(odontologoDb);

        Console.WriteLine("✅ Seed de Odontología: usuario, roles y categorías contables verificados");
    }

    private static async Task SeedAccountingCategoriesAsync(OdontologoDbContext db)
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
