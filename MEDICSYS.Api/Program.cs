using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;
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
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "MEDICSYS.Api")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddOpenApi();

    string GetRequiredConnectionString(string name)
    {
        var value = builder.Configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Connection string '{name}' is missing. Configure env var ConnectionStrings__{name}.");
        }
        return value;
    }

    Npgsql.NpgsqlDataSource BuildDataSource(string connectionString)
    {
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        return dataSourceBuilder.Build();
    }

    builder.Services.AddSingleton(new AppDbDataSource(BuildDataSource(GetRequiredConnectionString("DefaultConnection"))));
    builder.Services.AddSingleton(new AcademicDbDataSource(BuildDataSource(GetRequiredConnectionString("AcademicoConnection"))));
    builder.Services.AddSingleton(new OdontologoDbDataSource(BuildDataSource(GetRequiredConnectionString("OdontologiaConnection"))));

    // DbContext principal (historiales, agenda, pacientes, recordatorios)
    builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    {
        var dataSource = sp.GetRequiredService<AppDbDataSource>().DataSource;
        options.UseNpgsql(dataSource);
    });

    // DbContext para Sistema Académico (Profesor-Alumno)
    builder.Services.AddDbContext<AcademicDbContext>((sp, options) =>
    {
        var dataSource = sp.GetRequiredService<AcademicDbDataSource>().DataSource;
        options.UseNpgsql(dataSource);
    });

    // DbContext para Odontología (facturación, inventario, contabilidad)
    builder.Services.AddDbContext<OdontologoDbContext>((sp, options) =>
    {
        var dataSource = sp.GetRequiredService<OdontologoDbDataSource>().DataSource;
        options.UseNpgsql(dataSource);
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

    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    {
        throw new InvalidOperationException("Jwt key missing or too short. Configure env var Jwt__Key with at least 32 characters.");
    }
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

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("app-db")
        .AddDbContextCheck<AcademicDbContext>("academic-db")
        .AddDbContextCheck<OdontologoDbContext>("odontologia-db");

    builder.Services.AddRateLimiter(options =>
    {
        var permitLimit = builder.Configuration.GetValue<int?>("RateLimiting:PermitLimit") ?? 300;
        var windowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:WindowSeconds") ?? 60;
        var queueLimit = builder.Configuration.GetValue<int?>("RateLimiting:QueueLimit") ?? 0;

        permitLimit = Math.Clamp(permitLimit, 1, 10_000);
        windowSeconds = Math.Clamp(windowSeconds, 1, 3_600);
        queueLimit = Math.Max(0, queueLimit);

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var key = context.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "auth"
                : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
        });
    });

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
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            diagnosticContext.Set("UserId", httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier));
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
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
    app.Use(async (context, next) =>
    {
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        using (LogContext.PushProperty("UserId", context.User?.FindFirstValue(ClaimTypes.NameIdentifier)))
        using (LogContext.PushProperty("ClientIP", context.Connection.RemoteIpAddress?.ToString()))
        {
            await next();
        }
    });
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapHealthChecks("/health").DisableRateLimiting();
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
