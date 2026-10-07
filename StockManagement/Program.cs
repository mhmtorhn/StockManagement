using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StockManagement.Data;
using StockManagement.Services;
using StockManagement.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// Controller servisleri
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger ve JWT desteði
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "JWT token deðerini girin. " +
                "Baþýna Bearer yazmanýza gerek yoktur."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// Veritabaný
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

// Uygulama servisleri
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// JWT ayarlarý
string jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer yapýlandýrmasý bulunamadý.");

string jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience yapýlandýrmasý bulunamadý.");

string jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key yapýlandýrmasý bulunamadý.");

byte[] jwtKeyBytes = GetJwtKeyBytes(jwtKey);

// JWT Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(jwtKeyBytes),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero,

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();

// Login endpoint'i için IP bazlý rate limit
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "LoginPolicy",
        httpContext =>
        {
            string partitionKey =
                httpContext.Connection
                    .RemoteIpAddress?
                    .ToString()
                ?? "unknown";

            return RateLimitPartition
                .GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
        });
});

var app = builder.Build();

// Veritabanýnda kullanýcý yoksa ilk SuperAdmin'i oluþturur.
using (IServiceScope scope = app.Services.CreateScope())
{
    AdminSeeder adminSeeder =
        scope.ServiceProvider
            .GetRequiredService<AdminSeeder>();

    await adminSeeder.SeedAsync();
}

// HTTP istek hattý
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static byte[] GetJwtKeyBytes(string jwtKey)
{
    try
    {
        byte[] keyBytes =
            Convert.FromBase64String(jwtKey);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT anahtarý en az 32 byte olmalýdýr.");
        }

        return keyBytes;
    }
    catch (FormatException)
    {
        byte[] keyBytes =
            Encoding.UTF8.GetBytes(jwtKey);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT anahtarý en az 32 byte olmalýdýr.");
        }

        return keyBytes;
    }
}