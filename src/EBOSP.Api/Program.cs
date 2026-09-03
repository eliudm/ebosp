using System.Text;
using System.Threading.RateLimiting;
using EBOSP.Api;
using EBOSP.Api.Authorization;
using EBOSP.Api.Common;
using EBOSP.Api.Middleware;
using EBOSP.Application.Authorization;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.MasterData;
using EBOSP.Infrastructure.Common;
using EBOSP.Infrastructure.Identity;
using EBOSP.Infrastructure.MasterData;
using EBOSP.Infrastructure.Outbox;
using EBOSP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Structured logging: JSON in every environment so logs are machine-parseable and consistent
// between local, CI and Azure (dev guide §9). Scopes (correlation id, etc.) are included.
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
});

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing required configuration: ConnectionStrings:Default. " +
        "Set it via user-secrets locally or environment/Key Vault in deployed environments.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// The React frontend (frontend/) runs on a different origin in dev; configurable per environment
// since the deployed frontend origin differs from the local Vite dev server.
const string FrontendCorsPolicy = "frontend";
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:5174"]; // Vite auto-increments the port if 5173 is taken.
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// URL path API versioning (ADR-0003) - added now that Identity is the first real module.
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
    })
    .AddMvc()
    .AddApiExplorer();

// Identity module (dev guide §11): JWT bearer auth, permission-based authorization, and the
// repositories/services behind the auth/user/tenant endpoints.
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
var jwtSigningKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience) || string.IsNullOrWhiteSpace(jwtSigningKey))
{
    throw new InvalidOperationException(
        "Missing required configuration: Jwt:Issuer, Jwt:Audience, Jwt:SigningKey. " +
        "Set them via user-secrets locally or environment/Key Vault in deployed environments.");
}

var jwtOptions = new JwtOptions
{
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    SigningKey = jwtSigningKey,
    AccessTokenLifetimeMinutes = jwtSection.GetValue("AccessTokenLifetimeMinutes", 15),
    RefreshTokenLifetimeDays = jwtSection.GetValue("RefreshTokenLifetimeDays", 30),
};
builder.Services.AddSingleton(jwtOptions);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Dynamic permission policies (Api/Authorization/PermissionPolicyProvider.cs) - no per-permission
// policy registration needed here, unlike the plain AddAuthorization() this replaces.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization();

// Spec §14: "Apply rate limiting to authentication and sensitive endpoints." Partitioned per
// client IP - AddFixedWindowLimiter's simple overload creates a single *global* window shared by
// every caller, which would let one burst of legitimate traffic lock out every other user (or let
// one attacker trivially deny login to the whole platform), so each policy is registered via
// AddPolicy with an explicit per-IP partition key instead.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Permit limits are configurable (RateLimiting:<Policy>:PermitLimit) rather than hardcoded so
    // tests can raise them for functional suites unrelated to rate limiting, without fighting
    // RateLimiterOptions.AddPolicy's "policy already registered" guard from a second registration.
    AddPerIpFixedWindowPolicy(options, builder.Configuration, RateLimiterPolicies.Login, defaultPermitLimit: 20);
    AddPerIpFixedWindowPolicy(options, builder.Configuration, RateLimiterPolicies.PasswordReset, defaultPermitLimit: 10);
    AddPerIpFixedWindowPolicy(options, builder.Configuration, RateLimiterPolicies.TenantCreation, defaultPermitLimit: 5);
});

static void AddPerIpFixedWindowPolicy(RateLimiterOptions options, IConfiguration configuration, string policyName, int defaultPermitLimit)
{
    var permitLimit = configuration.GetValue($"RateLimiting:{policyName}:PermitLimit", defaultPermitLimit);
    options.AddPolicy(policyName, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
}

builder.Services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IMfaChallengeProvider, NoOpMfaChallengeProvider>();
builder.Services.AddScoped<IPasswordResetNotifier, LoggingPasswordResetNotifier>();
builder.Services.AddScoped<IDomainEventRecorder, DomainEventRecorder>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // HTTP Strict Transport Security - skipped in dev since the local http profile has no cert.
    app.UseHsts();
}

// Baseline security headers (spec §14 is HTTPS/authz/input-validation-focused; these are the
// standard, low-risk defense-in-depth headers beyond what it enumerates explicitly).
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    await next();
});

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Makes the top-level Program class visible to EBOSP.ApiTests' WebApplicationFactory<Program>.
public partial class Program;
