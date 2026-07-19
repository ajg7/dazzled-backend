using Dazzled.Api.Hubs;
using Dazzled.Application;
using Dazzled.Infrastructure;
using Dazzled.Infrastructure.Auth;
using Dazzled.Infrastructure.Data;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default");

// 1. Serilog (Console + Grafana Loki for centralized log search)
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.GrafanaLoki(
            context.Configuration["Loki:Url"] ?? "http://localhost:3100",
            labels: [new LokiLabel { Key = "app", Value = "dazzled-api" }]));

// 2 & 4. DbContext, MassTransit (+ outbox), token service, repositories
builder.Services.AddInfrastructure(builder.Configuration);

// Application orchestrators
builder.Services.AddApplication();

// OpenTelemetry metrics (scraped by Prometheus at /metrics)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("dazzled-api"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

// 3. JWT auth
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// 5. Hangfire (same DB, separate schema)
builder.Services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

// 6. Polly resilience for the Twilio HTTP client
builder.Services.AddHttpClient("twilio").AddStandardResilienceHandler();

// 7. SignalR
builder.Services.AddSignalR();

// 8. Health checks (RabbitMQ is covered by MassTransit's bus health check)
builder.Services.AddHealthChecks().AddSqlServer(connectionString!);

// 9. Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // "Authorize" button in Swagger UI: paste the token from /api/v1/auth/login
    // (raw JWT — the "Bearer " prefix is added automatically).
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned by /api/v1/auth/login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), [] }
    });
});

var app = builder.Build();

// 10. Run pending migrations when enabled
if (builder.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<DazzledDbContext>().Database.Migrate();
}

app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapPrometheusScrapingEndpoint(); // exposes /metrics for Prometheus
app.MapHub<IncidentHub>("/hubs/incidents");
app.UseHangfireDashboard("/hangfire");
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
