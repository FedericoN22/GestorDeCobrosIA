using System.Text;
using Kiosk.Api.Auth;
using Kiosk.Api.Middleware;
using Kiosk.Api.Seed;
using Kiosk.Application.CasosUso.Autenticacion;
using Kiosk.Domain.Usuarios;
using Kiosk.Application.CasosUso.Catalogos;
using Kiosk.Application.CasosUso.Intenciones;
using Kiosk.Application.CasosUso.Stock;
using Kiosk.Application.CasosUso.Ventas;
using Kiosk.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var database = builder.Configuration.GetSection("Database");
var useSqlite = string.Equals(database["Provider"], "sqlite", StringComparison.OrdinalIgnoreCase);
var connectionString = useSqlite
    ? builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=kiosk.db"
    : builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Falta la cadena de conexión 'Postgres' en la configuración.");

builder.Services.AddInfrastructure(new DbOptions(useSqlite, connectionString));

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddSingleton(new TokenService(
    jwt["SecretKey"] ?? throw new InvalidOperationException("Falta 'Jwt:SecretKey' en la configuración."),
    jwt["Issuer"] ?? throw new InvalidOperationException("Falta 'Jwt:Issuer' en la configuración."),
    jwt["Audience"] ?? throw new InvalidOperationException("Falta 'Jwt:Audience' en la configuración."),
    int.TryParse(jwt["ExpiresInMinutes"], out var minutos) ? minutos : 120));

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permiso in Permisos.Todos)
    {
        options.AddPolicy(permiso, policy => policy.RequireClaim("permiso", permiso));
    }
});

builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<ServicioCategorias>();
builder.Services.AddScoped<ServicioProductos>();
builder.Services.AddScoped<ServicioStock>();
builder.Services.AddScoped<ServicioCaja>();
builder.Services.AddScoped<ServicioVentas>();
builder.Services.AddScoped<ServicioIntenciones>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddHostedService<SeedWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<DomainExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
