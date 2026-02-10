using DtExpress.Api.Filters;
using DtExpress.Api.Middleware;
using DtExpress.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// === Service Registration (single entry point — all 5 domains) ===
builder.Services.AddDtExpress();

// === API Setup ===
builder.Services.AddControllers(options =>
{
    // Global exception filter — maps DomainException → HTTP status codes
    options.Filters.Add<GlobalExceptionFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DT-Express TMS API",
        Version = "v1",
        Description = "Transport Management System — Dynamic Routing, Multi-Carrier, Real-time Tracking, Order Processing, Audit Trail"
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

app.MapControllers();

app.Run();
