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

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
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
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);
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
