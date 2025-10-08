using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManagementAPI.Data;
using TaskManagementAPI.Repositories.Implementations;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services.Implementations;
using TaskManagementAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Define a policy name for CORS
const string AllowSpecificOrigin = "_allowSpecificOrigin";

// 1. Controllers + JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ensures enums are serialized as strings (e.g., "pending" instead of 0)
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
        );
    });

// 2. DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2.5. CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: AllowSpecificOrigin,
        policy =>
        {
            // Using AllowAnyOrigin for debugging, switch to specific origins in production
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// 3. DI: Repos & Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserServices>();
builder.Services.AddScoped<IJwtService, JwtServices>();


// 4. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. Authentication Setup
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// *** CRITICAL CONFIG LOGGING FOR DEBUGGING ***
if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    Console.WriteLine("CONFIG ERROR: One or more JWT configuration values (Key, Issuer, Audience) are missing or null.");
}
else
{
    Console.WriteLine($"JWT Config Loaded:");
    Console.WriteLine($"  Issuer: {jwtIssuer}");
    Console.WriteLine($"  Audience: {jwtAudience}");
    Console.WriteLine($"  Key Length: {jwtKey.Length}");
}

// Ensure key is present before proceeding
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key not found. Please check appsettings.json or appsettings.Development.json.");
}

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
// ****************************************************

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        // Small clock skew tolerance for server time differences
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // This message is critical. It will show the exact validation error (e.g., "Signature validation failed")
            Console.WriteLine("Auth failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated for: " + context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply CORS Policy - MUST be before UseAuthentication/UseAuthorization
app.UseCors(AllowSpecificOrigin);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
