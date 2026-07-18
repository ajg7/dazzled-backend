using Dazzled.Api.Hubs;
using Dazzled.Infrastructure;
using Dazzled.Infrastructure.Auth;
using Dazzled.Infrastructure.Data;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default");

// 1. Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

// 2 & 4. DbContext, MassTransit (+ outbox), token service
builder.Services.AddInfrastructure(builder.Configuration);

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
builder.Services.AddSwaggerGen();

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
app.MapHub<IncidentHub>("/hubs/incidents");
app.UseHangfireDashboard("/hangfire");
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
