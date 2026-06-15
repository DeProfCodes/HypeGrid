using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using HypeGrid.API.Json;
using HypeGrid.API.Middleware;
using HypeGrid.API.Services;
using HypeGrid.Application.Alerts;
using HypeGrid.Application.Analytics;
using HypeGrid.Application.Auth;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Application.Common.Interfaces.Shared;
using HypeGrid.Application.Communication.Email;
using HypeGrid.Application.Communication.Email.Interfaces;
using HypeGrid.Application.Communication.Email.Services;
using HypeGrid.Application.Communication.Email.Templates;
using HypeGrid.Application.Dashboard;
using HypeGrid.Application.Leads;
using HypeGrid.Application.Persistence.Identity;
using HypeGrid.Application.Storage;
using HypeGrid.Domain.Identity;
using HypeGrid.Infrastructure.Communications.Email;
using HypeGrid.Infrastructure.Configuration;
using HypeGrid.Infrastructure.Data;
using HypeGrid.Infrastructure.Data.Seed;
using HypeGrid.Infrastructure.Identity;
using HypeGrid.Infrastructure.Persistence;
using HypeGrid.Infrastructure.Services;
using HypeGrid.Infrastructure.Storage;
using HypeGrid.Shared.Constants;

namespace HypeGrid.API.Extensions;

/// <summary>
/// Centralised registration for API, infrastructure, auth, communication, and
/// middleware services. Mirrors the ZansiHustle composition-root layout.
/// </summary>
public static class ServiceExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // snake_case JSON so the existing Base44-built frontends bind to
                // the same field names (brand_name, created_date, ...) without
                // a rename. Dictionary keys (settings maps) pass through verbatim.
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "HypeGrid API",
                Version = "v1",
                Description = "Backend API for the HypeGrid public website and admin dashboard."
            });

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter: Bearer {your JWT}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }
            };
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
        });

        return services;
    }

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JwtSettings configuration is missing.");
        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
            throw new InvalidOperationException("JWT signing key is missing.");

        var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // dev convenience; enable in prod via reverse proxy/TLS
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(HypeGridPolicies.RequireAdminAccess, p => p.RequireRole(HypeGridRoles.AdminRoles));
            options.AddPolicy(HypeGridPolicies.RequireSuperAdmin, p => p.RequireRole(HypeGridRoles.SuperAdmin));
            options.AddPolicy(HypeGridPolicies.RequireFinance, p => p.RequireRole(HypeGridRoles.SuperAdmin, HypeGridRoles.Admin, HypeGridRoles.Finance));
            options.AddPolicy(HypeGridPolicies.RequireCampaignManager, p => p.RequireRole(HypeGridRoles.SuperAdmin, HypeGridRoles.Admin, HypeGridRoles.CampaignManager));
            options.AddPolicy(HypeGridPolicies.RequireClient, p => p.RequireRole(HypeGridRoles.Client));
            options.AddPolicy(HypeGridPolicies.RequireCreator, p => p.RequireRole(HypeGridRoles.Creator));
        });

        return services;
    }

    /// <summary>Registers infrastructure + application service implementations.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Generic repository (open generic) backs all admin CRUD.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPublicLeadService, PublicLeadService>();
        services.AddScoped<IAlertSubscriptionService, AlertSubscriptionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IHostEnvironmentAccessor, HostEnvironmentAccessor>();

        return services;
    }

    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSenderSettings>(configuration.GetSection(EmailSenderSettings.SectionName));

        var emailSettings = configuration.GetSection(HypeGridEmailSettings.SectionName).Get<HypeGridEmailSettings>()
                            ?? new HypeGridEmailSettings();
        services.AddSingleton(emailSettings);
        services.AddSingleton<HypeGridEmailTemplates>();

        services.AddScoped<IEmailSenderMapper, EmailSenderMapper>();
        services.AddScoped<IEmailProvider, SmtpEmailProvider>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddHostedService<EmailSenderConfigReporter>();
        return services;
    }

    /// <summary>
    /// Registers the asset (image) storage provider. Cloudflare R2 by default; set
    /// AssetStorage:Provider=Local for dev disk storage. Binds config as a singleton
    /// so the provider can report config errors at upload time without crashing
    /// startup when R2 keys are absent.
    /// </summary>
    public static IServiceCollection AddAssetStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(AssetStorageSettings.SectionName).Get<AssetStorageSettings>()
                       ?? new AssetStorageSettings();
        services.AddSingleton(settings);

        if (string.Equals(settings.Provider, "Local", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IAssetStorageService, LocalAssetStorageService>();
        else
            services.AddScoped<IAssetStorageService, CloudflareR2AssetStorageService>();

        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("FrontendCors", policy =>
            {
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
                else
                {
                    // ⚠️ TEMPORARY — PERMISSIVE CORS FOR DEPLOYMENT TESTING. ⚠️
                    // Open to any origin so the website + admin can be smoke-tested from
                    // Vercel preview/production URLs without per-URL allow-listing.
                    //
                    // This is SAFE w.r.t. auth: both frontends are Bearer-token only
                    // (Authorization header from localStorage) — there are NO cookies and
                    // NO withCredentials, so JWT auth is unaffected by this policy. Because
                    // we do NOT send credentials, AllowAnyOrigin() is permitted (it cannot
                    // be combined with AllowCredentials()).
                    //
                    // TODO(PROD HARDENING): LOCK THIS BACK DOWN before going live. Restore
                    // the env-driven allow-list below and limit origins to the final domains:
                    //   https://hypegrid.co.za
                    //   https://www.hypegrid.co.za
                    //   https://portal.hypegrid.co.za
                    //   https://admin.hypegrid.co.za
                    // See HypeGrid/docs/CORS_AND_DEPLOYMENT.md.
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();

                    // --- RESTORE FOR PRODUCTION (env-driven allow-list) -------------------
                    // Re-enable this block (and delete the permissive policy above) to lock
                    // CORS back down. Origins = built-in defaults + HYPEGRID_CORS_ORIGINS.
                    // NOTE: AllowCredentials() is only needed if the frontend ever switches
                    // to cookie auth; with Bearer-only it can be omitted.
                    //
                    // var defaultOrigins = new[]
                    // {
                    //     "http://localhost:5173", "https://localhost:5173",   // website (Vite default)
                    //     "http://localhost:5174", "https://localhost:5174",   // admin (second Vite app)
                    //     "http://localhost:3000", "http://localhost:8080",
                    //     "https://hypegrid.co.za", "https://www.hypegrid.co.za",
                    //     "https://portal.hypegrid.co.za", "https://admin.hypegrid.co.za",
                    // };
                    //
                    // var extraOrigins = (Environment.GetEnvironmentVariable("HYPEGRID_CORS_ORIGINS")
                    //         ?? string.Empty)
                    //     .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    //
                    // var origins = defaultOrigins
                    //     .Concat(extraOrigins)
                    //     .Select(o => o.TrimEnd('/'))
                    //     .Distinct(StringComparer.OrdinalIgnoreCase)
                    //     .ToArray();
                    //
                    // policy
                    //     .WithOrigins(origins)
                    //     .AllowAnyHeader().AllowAnyMethod();
                    // ---------------------------------------------------------------------
                }
            });
        });
        return services;
    }

    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseRouting();
        app.UseCors("FrontendCors");

        // Serve dev-mode local asset uploads at /uploads (no-op/empty in R2 mode).
        // Uses an explicit physical provider so it works regardless of WebRootPath.
        var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadsPath),
            RequestPath = "/uploads",
        });

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    /// <summary>
    /// Applies migrations (fatal on failure) then seeds roles / dev admin /
    /// website content (non-fatal on failure).
    /// </summary>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var db = services.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed during startup. Host will not start.");
            throw;
        }

        try
        {
            var db = services.GetRequiredService<AppDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var config = services.GetRequiredService<IConfiguration>();
            var env = services.GetRequiredService<IHostEnvironmentAccessor>();
            await HypeGridSeeder.SeedAsync(db, roleManager, userManager, config, env, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seeding failed during startup. Host will continue.");
        }
    }
}
