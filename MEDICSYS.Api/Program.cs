using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddOpenApi();

    // DbContext principal (historiales, agenda, pacientes, recordatorios)
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
        dataSourceBuilder.EnableDynamicJson();
        options.UseNpgsql(dataSourceBuilder.Build());
    });

    // DbContext para Sistema Académico (Profesor-Alumno)
    builder.Services.AddDbContext<AcademicDbContext>(options =>
    {
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("AcademicoConnection"));
        dataSourceBuilder.EnableDynamicJson();
        options.UseNpgsql(dataSourceBuilder.Build());
    });

    // DbContext para Odontología (facturación, inventario, contabilidad)
    builder.Services.AddDbContext<OdontologoDbContext>(options =>
    {
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("OdontologiaConnection"));
        dataSourceBuilder.EnableDynamicJson();
        options.UseNpgsql(dataSourceBuilder.Build());
    });

    // Identity usa el contexto académico
    builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AcademicDbContext>()
        .AddSignInManager();

    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt key missing.");
    var issuer = builder.Configuration["Jwt:Issuer"] ?? "MEDICSYS";
    var audience = builder.Configuration["Jwt:Audience"] ?? "MEDICSYS";

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorization();

    var corsOrigin = builder.Configuration["Cors:Origin"] ?? "http://localhost:4200";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WebApp", policy =>
        {
            policy.WithOrigins(corsOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddScoped<TokenService>();
    builder.Services.AddHostedService<ReminderWorker>();
    builder.Services.Configure<SriOptions>(builder.Configuration.GetSection("Sri"));
    builder.Services.AddScoped<ISriService, SriService>();

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        using var scope = app.Services.CreateScope();
        
        // Migrar bases de datos
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await appDb.Database.MigrateAsync();

        var academicDb = scope.ServiceProvider.GetRequiredService<AcademicDbContext>();
        await academicDb.Database.MigrateAsync();

        var odontologoDb = scope.ServiceProvider.GetRequiredService<OdontologoDbContext>();
        await odontologoDb.Database.MigrateAsync();

        // Poblar datos de prueba
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await AcademicSeedData.SeedAsync(academicDb, userManager);
        await OdontologoSeedData.SeedAsync(odontologoDb, userManager);

        // Sincronización de usuarios se ejecuta fuera del bloque de entorno
    }
    else
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                if (exceptionFeature != null)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(exceptionFeature.Error, "Unhandled exception at {Path}", exceptionFeature.Path);
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Ha ocurrido un error inesperado." });
            });
        });
    }

    await SyncAcademicUsersAsync(app.Services);

    app.UseCors("WebApp");
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    await app.RunAsync();

    static async Task SyncAcademicUsersAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var academicDb = scope.ServiceProvider.GetRequiredService<AcademicDbContext>();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var academicUsers = await academicDb.Users.AsNoTracking().ToListAsync();
        var appUsers = await appDb.Users.ToListAsync();
        var appUsersById = appUsers.ToDictionary(u => u.Id, u => u);

        foreach (var user in academicUsers)
        {
            if (appUsersById.TryGetValue(user.Id, out var existing))
            {
                existing.FullName = user.FullName;
                existing.UniversityId = user.UniversityId;
                existing.UserName = user.UserName;
                existing.NormalizedUserName = user.NormalizedUserName;
                existing.Email = user.Email;
                existing.NormalizedEmail = user.NormalizedEmail;
                existing.EmailConfirmed = user.EmailConfirmed;
                existing.PasswordHash = user.PasswordHash;
                existing.SecurityStamp = user.SecurityStamp;
                existing.ConcurrencyStamp = user.ConcurrencyStamp;
                existing.PhoneNumber = user.PhoneNumber;
                existing.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
                existing.TwoFactorEnabled = user.TwoFactorEnabled;
                existing.LockoutEnd = user.LockoutEnd;
                existing.LockoutEnabled = user.LockoutEnabled;
                existing.AccessFailedCount = user.AccessFailedCount;
            }
            else
            {
                appDb.Users.Add(new ApplicationUser
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    UniversityId = user.UniversityId,
                    UserName = user.UserName,
                    NormalizedUserName = user.NormalizedUserName,
                    Email = user.Email,
                    NormalizedEmail = user.NormalizedEmail,
                    EmailConfirmed = user.EmailConfirmed,
                    PasswordHash = user.PasswordHash,
                    SecurityStamp = user.SecurityStamp,
                    ConcurrencyStamp = user.ConcurrencyStamp,
                    PhoneNumber = user.PhoneNumber,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnd = user.LockoutEnd,
                    LockoutEnabled = user.LockoutEnabled,
                    AccessFailedCount = user.AccessFailedCount
                });
            }
        }

        await appDb.SaveChangesAsync();
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "MEDICSYS Api finalizó por una excepción no controlada");
}
finally
{
    Log.CloseAndFlush();
}
