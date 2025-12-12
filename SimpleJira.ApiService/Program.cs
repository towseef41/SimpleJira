using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SimpleJira.ApiService.Data;
using SimpleJira.ApiService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console();
});

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// ✅ ADD THIS
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Already present
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Title ??= "Unexpected error";
        ctx.ProblemDetails.Status ??= StatusCodes.Status500InternalServerError;
        if (!builder.Environment.IsDevelopment())
        {
            ctx.ProblemDetails.Detail = null;
        }
    };
});

builder.Services.AddDbContext<JiraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("simplejira")));

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
    new[] { "http://localhost:5249", "https://localhost:5249", "http://localhost:5000", "https://localhost:5001" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var authEnabled = builder.Configuration.GetValue("Auth:Enabled", true);

if (authEnabled)
{
    // JWT Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"] ?? "simplejira";
var audience = jwtSection["Audience"] ?? "simplejira-web";
var key = jwtSection["Key"] ?? "dev-secret-change-me";
var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32, '0')); // ensure at least 256 bits
var signingKey = new SymmetricSecurityKey(keyBytes);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = signingKey
            };
        });
    builder.Services.AddAuthorization();
}
else
{
    builder.Services.AddAuthentication("AllowAll")
        .AddScheme<AuthenticationSchemeOptions, SimpleJira.ApiService.Infrastructure.AllowAllAuthHandler>("AllowAll", null);
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = options.DefaultPolicy;
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/error");
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// ✅ ADD THIS
app.MapControllers();

app.Map("/error", (HttpContext httpContext) =>
{
    var exception = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
    return Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Unexpected error",
        detail: builder.Environment.IsDevelopment() ? exception?.ToString() : null);
}).ExcludeFromDescription();

app.MapDefaultEndpoints();
app.Run();
