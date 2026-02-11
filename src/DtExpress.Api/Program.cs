using System.Text;
using DtExpress.Api.Auth;
using DtExpress.Api.Filters;
using DtExpress.Api.Middleware;
using DtExpress.Application.Auth.Models;
using DtExpress.Application.Auth.Services;
using DtExpress.Infrastructure.Data;
using DtExpress.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// === Service Registration (single entry point — all 5 domains) ===
builder.Services.AddDtExpress();

// === Database Registration (EF Core + PostgreSQL) ===
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDtExpressData(connectionString);

// === JWT Settings ===
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings section 'Jwt' not found in configuration.");
builder.Services.AddSingleton(jwtSettings);

// === Authentication (JWT Bearer) ===
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
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.FromSeconds(30), // Tight clock skew for 15min tokens
    };
});
builder.Services.AddAuthorization();

// === Current User Context ===
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// === API Setup ===
builder.Services.AddControllers(options =>
{
    // Global exception filter — maps DomainException → HTTP status codes
    options.Filters.Add<GlobalExceptionFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DT-Express TMS API",
        Version = "v1",
        Description = """
            Transport Management System — Dynamic Routing, Multi-Carrier, Real-time Tracking, Order Processing, Audit Trail.

            ## Authentication
            Use **POST /api/auth/login** to get a JWT token, then click the 🔒 **Authorize** button above and enter: `Bearer {token}`

            ## Test Accounts (seeded)
            | Username | Password | Role |
            |---|---|---|
            | admin | admin123 | Admin (full access) |
            | dispatcher | passwd123 | Dispatcher (orders + carriers) |
            | driver | passwd123 | Driver (deliver orders) |
            | viewer | passwd123 | Viewer (read-only) |

            ## Domains
            - **Orders** — Full lifecycle: Created → Confirmed → Shipped → Delivered / Cancelled
            - **Routing** — Strategy-based route calculation (Fastest / Cheapest / Balanced)
            - **Carriers** — Multi-carrier quotes and booking (SF Express 顺丰, JD Logistics 京东)
            - **Tracking** — Real-time shipment tracking with observer pattern
            - **Audit** — Immutable audit trail with correlation ID tracing

            ## Chinese Data Support
            All text fields support Chinese characters (UTF-8). Use Chinese addresses (上海, 北京, 广州), names (张三, 李四), and weight units (Kg, G, Jin, Lb).
            """,
        Contact = new OpenApiContact
        {
            Name = "DT-Express Team",
            Email = "dev@dtexpress.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT"
        }
    });

    // JWT Bearer auth in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Group endpoints by controller tag
    options.TagActionsBy(api =>
        new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"]! });

    // Include XML documentation for Swagger UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// === Middleware Pipeline ===

// Correlation ID — reads/generates X-Correlation-ID header for request tracing
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DT-Express TMS API v1");
        options.DocumentTitle = "DT-Express API Documentation";
    });
}

app.UseHttpsRedirection();

// Authentication & Authorization (must be before MapControllers)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the implicit Program class public so test projects can reference it
// via WebApplicationFactory<Program>
public partial class Program { }
