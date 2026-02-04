using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Odontologia;
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

        // Verificar si ya hay datos
        if (odontologoDb.OdontologoPatients.Any()) return;

        // Crear 5 pacientes
        var patients = new List<OdontologoPatient>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                FirstName = "María",
                LastName = "González",
                IdNumber = "1234567890",
                DateOfBirth = "1985-03-15",
                Gender = "F",
                Address = "Av. 10 de Agosto 123, Quito",
                Phone = "0999123456",
                Email = "maria.gonzalez@email.com",
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                FirstName = "Juan",
                LastName = "Pérez",
                IdNumber = "0987654321",
                DateOfBirth = "1990-07-22",
                Gender = "M",
                Address = "Calle García Moreno 456, Quito",
                Phone = "0998765432",
                Email = "juan.perez@email.com",
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UpdatedAt = DateTime.UtcNow.AddMonths(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                FirstName = "Ana",
                LastName = "Martínez",
                IdNumber = "1122334455",
                DateOfBirth = "1988-11-30",
                Gender = "F",
                Address = "Av. América N45-123, Quito",
                Phone = "0997654321",
                Email = "ana.martinez@email.com",
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UpdatedAt = DateTime.UtcNow.AddMonths(-4)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                FirstName = "Carlos",
                LastName = "López",
                IdNumber = "5544332211",
                DateOfBirth = "1995-05-18",
                Gender = "M",
                Address = "Calle Colón E5-67, Quito",
                Phone = "0996543210",
                Email = "carlos.lopez@email.com",
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow.AddMonths(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                FirstName = "Laura",
                LastName = "Ramírez",
                IdNumber = "6677889900",
                DateOfBirth = "1992-09-25",
                Gender = "F",
                Address = "Av. 6 de Diciembre N34-890, Quito",
                Phone = "0995432109",
                Email = "laura.ramirez@email.com",
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatedAt = DateTime.UtcNow.AddMonths(-2)
            }
        };

        odontologoDb.OdontologoPatients.AddRange(patients);
        await odontologoDb.SaveChangesAsync();

        // Crear citas para cada paciente
        var appointments = new List<OdontologoAppointment>();
        foreach (var patient in patients)
        {
            appointments.Add(new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                Reason = "Limpieza dental",
                StartAt = DateTime.UtcNow.AddDays(-30),
                EndAt = DateTime.UtcNow.AddDays(-30).AddHours(1),
                Status = AppointmentStatus.Completed,
                Notes = "Limpieza realizada satisfactoriamente",
                CreatedAt = DateTime.UtcNow.AddDays(-31),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            });

            appointments.Add(new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                Reason = "Consulta de control",
                StartAt = DateTime.UtcNow.AddDays(7),
                EndAt = DateTime.UtcNow.AddDays(7).AddHours(1),
                Status = AppointmentStatus.Confirmed,
                Notes = "Control post-limpieza",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        odontologoDb.OdontologoAppointments.AddRange(appointments);
        await odontologoDb.SaveChangesAsync();

        // Crear historias clínicas
        var histories = new List<OdontologoClinicalHistory>();
        foreach (var patient in patients)
        {
            var historyData = new JsonObject
            {
                ["personal"] = new JsonObject
                {
                    ["firstName"] = patient.FirstName,
                    ["lastName"] = patient.LastName,
                    ["idNumber"] = patient.IdNumber,
                    ["dateOfBirth"] = patient.DateOfBirth,
                    ["gender"] = patient.Gender,
                    ["address"] = patient.Address,
                    ["phone"] = patient.Phone,
                    ["email"] = patient.Email
                },
                ["medicalHistory"] = new JsonObject
                {
                    ["allergies"] = "Ninguna",
                    ["medications"] = "Ninguna",
                    ["conditions"] = "Ninguna"
                },
                ["diagnosis"] = "Caries dental leve en molar inferior derecho",
                ["treatment"] = "Obturación con resina",
                ["observations"] = "Paciente colaborador, buena higiene oral"
            };

            histories.Add(new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                PatientIdNumber = patient.IdNumber,
                Data = historyData,
                Status = ClinicalHistoryStatus.Approved,
                CreatedAt = patient.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            });
        }

        odontologoDb.OdontologoClinicalHistories.AddRange(histories);
        await odontologoDb.SaveChangesAsync();

        // Crear facturas
        var invoices = new List<Invoice>();
        decimal invoiceNumber = 1;
        foreach (var patient in patients.Take(3))
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                Number = $"001-001-{invoiceNumber:000000000}",
                EstablishmentCode = "001",
                EmissionPoint = "001",
                Sequential = (int)invoiceNumber,
                IssuedAt = DateTime.UtcNow.AddDays(-15),
                CustomerIdentificationType = "05",
                CustomerIdentification = patient.IdNumber,
                CustomerName = $"{patient.FirstName} {patient.LastName}",
                CustomerAddress = patient.Address,
                CustomerPhone = patient.Phone,
                CustomerEmail = patient.Email,
                Subtotal = 100.00m,
                DiscountTotal = 0,
                Tax = 15.00m,
                Total = 115.00m,
                CardFeePercent = null,
                CardFeeAmount = null,
                TotalToCharge = 115.00m,
                PaymentMethod = PaymentMethod.Cash,
                Status = InvoiceStatus.Authorized,
                SriAuthorizationNumber = $"1234567890{invoiceNumber}",
                SriAuthorizedAt = DateTime.UtcNow.AddDays(-15),
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            };

            var items = new List<InvoiceItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = "Limpieza dental profesional",
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    DiscountPercent = 0,
                    Subtotal = 50.00m,
                    TaxRate = 15.00m,
                    Tax = 7.50m,
                    Total = 57.50m
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = "Obturación dental",
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    DiscountPercent = 0,
                    Subtotal = 50.00m,
                    TaxRate = 15.00m,
                    Tax = 7.50m,
                    Total = 57.50m
                }
            };

            invoices.Add(invoice);
            invoiceNumber++;
        }

        odontologoDb.Invoices.AddRange(invoices);
        await odontologoDb.SaveChangesAsync();

        // Crear categorías contables
        var categories = new List<AccountingCategory>
        {
            new() { Id = Guid.NewGuid(), Name = "Servicios Odontológicos", Type = AccountingEntryType.Income, Group = "Ingresos Principales" },
            new() { Id = Guid.NewGuid(), Name = "Materiales Dentales", Type = AccountingEntryType.Expense, Group = "Gastos Operativos" },
            new() { Id = Guid.NewGuid(), Name = "Suministros de Oficina", Type = AccountingEntryType.Expense, Group = "Gastos Administrativos" },
            new() { Id = Guid.NewGuid(), Name = "Servicios Públicos", Type = AccountingEntryType.Expense, Group = "Gastos Fijos" },
            new() { Id = Guid.NewGuid(), Name = "Salarios", Type = AccountingEntryType.Expense, Group = "Gastos de Personal" }
        };

        odontologoDb.AccountingCategories.AddRange(categories);
        await odontologoDb.SaveChangesAsync();

        // Crear entradas contables
        var entries = new List<AccountingEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = categories[0].Id,
                Description = "Servicios odontológicos - Enero 2026",
                Amount = 1500.00m,
                Type = AccountingEntryType.Income,
                Date = DateTime.SpecifyKind(new DateTime(2026, 1, 31), DateTimeKind.Utc),
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 31), DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = categories[1].Id,
                Description = "Compra de resinas y amalgamas",
                Amount = 300.00m,
                Type = AccountingEntryType.Expense,
                Date = DateTime.SpecifyKind(new DateTime(2026, 1, 15), DateTimeKind.Utc),
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 15), DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = categories[2].Id,
                Description = "Papelería y suministros",
                Amount = 50.00m,
                Type = AccountingEntryType.Expense,
                Date = DateTime.SpecifyKind(new DateTime(2026, 1, 10), DateTimeKind.Utc),
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 10), DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = categories[3].Id,
                Description = "Agua, luz, internet - Enero",
                Amount = 120.00m,
                Type = AccountingEntryType.Expense,
                Date = DateTime.SpecifyKind(new DateTime(2026, 1, 31), DateTimeKind.Utc),
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 31), DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = categories[4].Id,
                Description = "Salarios - Enero 2026",
                Amount = 800.00m,
                Type = AccountingEntryType.Expense,
                Date = DateTime.SpecifyKind(new DateTime(2026, 1, 31), DateTimeKind.Utc),
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 31), DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CategoryId = categories[0].Id,
                Description = "Servicios odontológicos - Febrero 2026",
                Amount = 345.00m,
                Type = AccountingEntryType.Income,
                Date = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };

        odontologoDb.AccountingEntries.AddRange(entries);
        await odontologoDb.SaveChangesAsync();

        // Crear items de inventario con algunas alertas
        Console.WriteLine("Creando items de inventario...");
        var inventoryItems = new List<InventoryItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                Name = "Guantes de látex",
                Description = "Caja de 100 unidades",
                Sku = "GLV-LAT-100",
                Quantity = 5,
                MinimumQuantity = 10,
                UnitPrice = 15.00m,
                Supplier = "Suministros Médicos S.A.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                Name = "Mascarillas desechables",
                Description = "Caja de 50 unidades",
                Sku = "MSK-DES-50",
                Quantity = 0,
                MinimumQuantity = 5,
                UnitPrice = 8.00m,
                Supplier = "Suministros Médicos S.A.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                Name = "Amalgama dental",
                Description = "Cápsulas de amalgama",
                Sku = "AML-DEN-CAP",
                Quantity = 20,
                MinimumQuantity = 10,
                UnitPrice = 2.50m,
                Supplier = "Dentrix Pro",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                Name = "Anestesia local",
                Description = "Lidocaína 2%",
                Sku = "ANS-LID-2",
                Quantity = 12,
                MinimumQuantity = 8,
                UnitPrice = 3.50m,
                Supplier = "Farmacia Dental",
                ExpirationDate = DateTime.UtcNow.AddDays(15),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                Name = "Resina compuesta",
                Description = "Kit de resina para obturaciones",
                Sku = "RES-COM-KIT",
                Quantity = 8,
                MinimumQuantity = 5,
                UnitPrice = 45.00m,
                Supplier = "Dentrix Pro",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                Name = "Hilo dental",
                Description = "Rollo de 50 metros",
                Sku = "HIL-DEN-50",
                Quantity = 25,
                MinimumQuantity = 15,
                UnitPrice = 1.20m,
                Supplier = "Suministros Médicos S.A.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologo.Id,
                Name = "Ácido grabador",
                Description = "Gel de ácido fosfórico 37%",
                Sku = "ACD-GRB-37",
                Quantity = 3,
                MinimumQuantity = 6,
                UnitPrice = 12.00m,
                Supplier = "Dentrix Pro",
                ExpirationDate = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        odontologoDb.InventoryItems.AddRange(inventoryItems);
        await odontologoDb.SaveChangesAsync();

        // Crear alertas de inventario
        Console.WriteLine("Creando alertas de inventario...");
        var expDateWarning = inventoryItems[3].ExpirationDate.GetValueOrDefault(DateTime.UtcNow);
        var expDateExpired = inventoryItems[6].ExpirationDate.GetValueOrDefault(DateTime.UtcNow);
        var alerts = new List<InventoryAlert>
        {
            new()
            {
                Id = Guid.NewGuid(),
                InventoryItemId = inventoryItems[0].Id,
                OdontologoId = odontologo.Id,
                Type = AlertType.LowStock,
                Message = $"Stock bajo de '{inventoryItems[0].Name}'. Stock actual: {inventoryItems[0].Quantity}, Mínimo: {inventoryItems[0].MinimumQuantity}",
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                InventoryItemId = inventoryItems[1].Id,
                OdontologoId = odontologo.Id,
                Type = AlertType.OutOfStock,
                Message = $"El artículo '{inventoryItems[1].Name}' está agotado. Stock: 0",
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                InventoryItemId = inventoryItems[3].Id,
                OdontologoId = odontologo.Id,
                Type = AlertType.ExpirationWarning,
                Message = $"El artículo '{inventoryItems[3].Name}' expirará en {(expDateWarning - DateTime.UtcNow).Days} días ({expDateWarning:dd/MM/yyyy})",
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                InventoryItemId = inventoryItems[6].Id,
                OdontologoId = odontologo.Id,
                Type = AlertType.Expired,
                Message = $"El artículo '{inventoryItems[6].Name}' expiró el {expDateExpired:dd/MM/yyyy}",
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                InventoryItemId = inventoryItems[6].Id,
                OdontologoId = odontologo.Id,
                Type = AlertType.LowStock,
                Message = $"Stock bajo de '{inventoryItems[6].Name}'. Stock actual: {inventoryItems[6].Quantity}, Mínimo: {inventoryItems[6].MinimumQuantity}",
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        odontologoDb.InventoryAlerts.AddRange(alerts);
        await odontologoDb.SaveChangesAsync();

        Console.WriteLine("✅ Base de datos Odontología poblada con datos de prueba");
    }
}
