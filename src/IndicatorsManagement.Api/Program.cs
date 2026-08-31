using System.IO.Compression;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using IndicatorsManagement.Api.Authorization;
using IndicatorsManagement.Api.Extensions;
using IndicatorsManagement.Api.Middleware;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Infrastructure.Data;
using IndicatorsManagement.Infrastructure.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration Guard ---
// Nothing below can work without connection strings and a signing key, so fail here
// with an actionable message rather than deep inside a provider.
builder.ValidateRequiredConfiguration();

// --- TLS 1.2+ Enforcement ---
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(https =>
    {
        https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});

// --- Serilog ---
// The database sink is opt-in (Serilog:WriteToDatabase). Local development writes to
// console and file only, so logging never depends on the database being reachable.
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);

    if (context.Configuration.GetValue("Serilog:WriteToDatabase", false))
    {
        config.WriteTo.MSSqlServer(
            connectionString: context.Configuration.GetConnectionString("DefaultConnection"),
            sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
            {
                TableName = "SerilogLogs",
                AutoCreateSqlTable = true
            });
    }
});

// --- Database ---
builder.Services.AddDbContext<IndicatorsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- ASP.NET Core Identity ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<IndicatorsDbContext>()
.AddDefaultTokenProviders();

// --- JWT Authentication ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// --- Authorization Policies ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.SuperAdminOnly, policy =>
        policy.RequireRole(Roles.SuperAdmin));

    options.AddPolicy(PolicyNames.MinistryLevel, policy =>
        policy.RequireRole(Roles.SuperAdmin, Roles.MinistryAdmin));

    options.AddPolicy(PolicyNames.EntityLevel, policy =>
        policy.RequireRole(Roles.SuperAdmin, Roles.MinistryAdmin, Roles.EntityAdmin));

    options.AddPolicy(PolicyNames.DataEntry, policy =>
        policy.RequireRole(Roles.SuperAdmin, Roles.EntityAdmin, Roles.DataEntryUser));

    options.AddPolicy(PolicyNames.ReviewerAccess, policy =>
        policy.RequireRole(Roles.SuperAdmin, Roles.MinistryAdmin, Roles.EntityAdmin, Roles.Reviewer));

    options.AddPolicy(PolicyNames.AuditorAccess, policy =>
        policy.RequireRole(Roles.SuperAdmin, Roles.Auditor));

    options.AddPolicy(PolicyNames.EntityScoped, policy =>
        policy.AddRequirements(new EntityAccessRequirement()));

    options.AddPolicy(PolicyNames.ViewerAccess, policy =>
        policy.RequireRole(Roles.Viewer));

    options.AddPolicy(PolicyNames.PublishAccess, policy =>
        policy.RequireRole(Roles.SuperAdmin, Roles.MinistryAdmin));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, EntityAccessHandler>();

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- FluentValidation ---
builder.Services.AddValidatorsFromAssemblyContaining<IndicatorsManagement.Application.Placeholder>();

// --- Hangfire ---
// Hangfire creates its schema inside an existing database but never the database itself,
// so a fresh server would fail with SQL error 4060 before the app finishes starting.
SqlServerDatabaseBootstrapper.EnsureDatabaseExists(builder.Configuration.GetConnectionString("HangfireConnection")!);

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
});
GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = [60, 300, 900] });

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "نظام إدارة المؤشرات - Indicators Management API",
        Version = "v1",
        Description = "API for the Indicators Management System - Ministry of Economy and Trade"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
    });

    // Include XML documentation comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// --- Application Services ---
builder.Services.AddApplicationServices();

// --- Rate Limiting ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("general", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 10;
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// --- Response Compression ---
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.SmallestSize);

// --- Health Checks ---
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "sqlserver");

builder.Services.AddControllers();

var app = builder.Build();

// --- Middleware Pipeline ---
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseResponseCompression();

// S9 — security headers apply to every response, including error paths.
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Indicators API v1"));
}
else
{
    // HSTS outside Development so browsers refuse plaintext for one year.
    app.UseHsts();
}

// O3 — trust forwarded headers when running behind a reverse proxy so client IPs
// (used by rate limiter and audit log) are the real originating addresses.
app.UseForwardedHeaders(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
    KnownIPNetworks = { new(System.Net.IPAddress.Loopback, 32) },
    ForwardLimit = 2
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SessionValidationMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthFilter()],
    DashboardTitle = "نظام إدارة المؤشرات - Background Jobs"
});
app.MapControllers();
app.MapHealthChecks("/health");

// --- Seed Database ---
// O2 — auto-migration & seeding only when explicitly enabled or in Development.
var runMigrations = app.Configuration.GetValue<bool?>("Database:MigrateOnStartup")
                    ?? app.Environment.IsDevelopment();
if (runMigrations)
{
    await DatabaseSeeder.SeedAsync(app.Services);
}
else
{
    app.Logger.LogInformation(
        "Skipping DatabaseSeeder because Database:MigrateOnStartup is disabled. Run 'dotnet ef database update' manually.");
}

// --- Hangfire Recurring Jobs ---
RecurringJob.AddOrUpdate<DueDateNotificationJob>("due-date-notifications", job => job.ExecuteAsync(), "0 8 * * *"); // Daily at 8 AM
RecurringJob.AddOrUpdate<OverdueNotificationJob>("overdue-notifications", job => job.ExecuteAsync(), "0 9 * * *"); // Daily at 9 AM
RecurringJob.AddOrUpdate<SessionCleanupJob>("session-cleanup", job => job.ExecuteAsync(), Cron.Hourly); // Hourly
RecurringJob.AddOrUpdate<DraftCleanupJob>("draft-cleanup", job => job.ExecuteAsync(), Cron.Weekly); // Weekly

app.Run();
