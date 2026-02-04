using System.Security.Claims;
using System.Text;
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

    builder.Services.AddControllers();
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

        // Sincronizar usuarios del contexto académico al contexto principal
        var academicUsers = await academicDb.Users.AsNoTracking().ToListAsync();
        var appUsers = await appDb.Users.AsNoTracking().ToListAsync();
        var appUserIds = new HashSet<Guid>(appUsers.Select(u => u.Id));
        var appUserNames = new HashSet<string>(appUsers.Select(u => u.NormalizedUserName ?? string.Empty));
        var appEmails = new HashSet<string>(appUsers.Select(u => u.NormalizedEmail ?? string.Empty));

        var missingUsers = academicUsers
            .Where(u => !appUserIds.Contains(u.Id))
            .Where(u => string.IsNullOrWhiteSpace(u.NormalizedUserName) || !appUserNames.Contains(u.NormalizedUserName))
            .Where(u => string.IsNullOrWhiteSpace(u.NormalizedEmail) || !appEmails.Contains(u.NormalizedEmail))
            .Select(u => new ApplicationUser
            {
                Id = u.Id,
                FullName = u.FullName,
                UniversityId = u.UniversityId,
                UserName = u.UserName,
                NormalizedUserName = u.NormalizedUserName,
                Email = u.Email,
                NormalizedEmail = u.NormalizedEmail,
                EmailConfirmed = u.EmailConfirmed,
                PasswordHash = u.PasswordHash,
                SecurityStamp = u.SecurityStamp,
                ConcurrencyStamp = u.ConcurrencyStamp,
                PhoneNumber = u.PhoneNumber,
                PhoneNumberConfirmed = u.PhoneNumberConfirmed,
                TwoFactorEnabled = u.TwoFactorEnabled,
                LockoutEnd = u.LockoutEnd,
                LockoutEnabled = u.LockoutEnabled,
                AccessFailedCount = u.AccessFailedCount
            })
            .ToList();

        if (missingUsers.Count > 0)
        {
            appDb.Users.AddRange(missingUsers);
            await appDb.SaveChangesAsync();
        }
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

    app.UseCors("WebApp");
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MEDICSYS Api finalizó por una excepción no controlada");
}
finally
{
    Log.CloseAndFlush();
}
