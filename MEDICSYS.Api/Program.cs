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
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    });

    // DbContext para Sistema Académico (Profesor-Alumno)
    builder.Services.AddDbContext<AcademicDbContext>((sp, options) =>
    {
        var dataSource = sp.GetRequiredService<AcademicDbDataSource>().DataSource;
        options.UseNpgsql(dataSource);
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    });

    // DbContext para Odontología (facturación, inventario, contabilidad)
    builder.Services.AddDbContext<OdontologoDbContext>((sp, options) =>
    {
        var dataSource = sp.GetRequiredService<OdontologoDbDataSource>().DataSource;
        options.UseNpgsql(dataSource);
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
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
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Migrar bases de datos
        await MigrateWithPendingModelToleranceAsync<AppDbContext>(scope.ServiceProvider, startupLogger);
        await MigrateWithPendingModelToleranceAsync<AcademicDbContext>(scope.ServiceProvider, startupLogger);
        await MigrateWithPendingModelToleranceAsync<OdontologoDbContext>(scope.ServiceProvider, startupLogger);

        // Poblar datos de prueba
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var academicDb = scope.ServiceProvider.GetRequiredService<AcademicDbContext>();
        var odontologoDb = scope.ServiceProvider.GetRequiredService<OdontologoDbContext>();
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
        var appUsersByNormalizedUserName = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);
        var appUsersByNormalizedEmail = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var appUser in appUsers)
        {
            if (!string.IsNullOrWhiteSpace(appUser.NormalizedUserName) &&
                !appUsersByNormalizedUserName.ContainsKey(appUser.NormalizedUserName))
            {
                appUsersByNormalizedUserName[appUser.NormalizedUserName] = appUser;
            }

            if (!string.IsNullOrWhiteSpace(appUser.NormalizedEmail) &&
                !appUsersByNormalizedEmail.ContainsKey(appUser.NormalizedEmail))
            {
                appUsersByNormalizedEmail[appUser.NormalizedEmail] = appUser;
            }
        }

        static void CopyAcademicUserToAppUser(ApplicationUser source, ApplicationUser target)
        {
            target.FullName = source.FullName;
            target.UniversityId = source.UniversityId;
            target.UserName = source.UserName;
            target.NormalizedUserName = source.NormalizedUserName;
            target.Email = source.Email;
            target.NormalizedEmail = source.NormalizedEmail;
            target.EmailConfirmed = source.EmailConfirmed;
            target.PasswordHash = source.PasswordHash;
            target.SecurityStamp = source.SecurityStamp;
            target.ConcurrencyStamp = source.ConcurrencyStamp;
            target.PhoneNumber = source.PhoneNumber;
            target.PhoneNumberConfirmed = source.PhoneNumberConfirmed;
            target.TwoFactorEnabled = source.TwoFactorEnabled;
            target.LockoutEnd = source.LockoutEnd;
            target.LockoutEnabled = source.LockoutEnabled;
            target.AccessFailedCount = source.AccessFailedCount;
        }

        foreach (var user in academicUsers)
        {
            ApplicationUser? existing = null;
            if (appUsersById.TryGetValue(user.Id, out var existingById))
            {
                existing = existingById;
            }
            else if (!string.IsNullOrWhiteSpace(user.NormalizedUserName) &&
                     appUsersByNormalizedUserName.TryGetValue(user.NormalizedUserName, out var existingByUserName))
            {
                existing = existingByUserName;
            }
            else if (!string.IsNullOrWhiteSpace(user.NormalizedEmail) &&
                     appUsersByNormalizedEmail.TryGetValue(user.NormalizedEmail, out var existingByEmail))
            {
                existing = existingByEmail;
            }

            if (existing is not null)
            {
                CopyAcademicUserToAppUser(user, existing);
            }
            else
            {
                var newUser = new ApplicationUser
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
                };

                appDb.Users.Add(newUser);
                appUsersById[newUser.Id] = newUser;

                if (!string.IsNullOrWhiteSpace(newUser.NormalizedUserName) &&
                    !appUsersByNormalizedUserName.ContainsKey(newUser.NormalizedUserName))
                {
                    appUsersByNormalizedUserName[newUser.NormalizedUserName] = newUser;
                }

                if (!string.IsNullOrWhiteSpace(newUser.NormalizedEmail) &&
                    !appUsersByNormalizedEmail.ContainsKey(newUser.NormalizedEmail))
                {
                    appUsersByNormalizedEmail[newUser.NormalizedEmail] = newUser;
                }
            }
        }

        await appDb.SaveChangesAsync();
    }

    static async Task MigrateWithPendingModelToleranceAsync<TContext>(
        IServiceProvider services,
        Microsoft.Extensions.Logging.ILogger<Program> logger) where TContext : DbContext
    {
        var db = services.GetRequiredService<TContext>();
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                ex,
                "Se omitio la migracion automatica para {DbContext} por cambios pendientes de modelo.",
                typeof(TContext).Name);
        }
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
