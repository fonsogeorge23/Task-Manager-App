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
using TaskManagementAPI.Static;

var builder = WebApplication.CreateBuilder(args);
var key = "Your_Super_Smart_SecretKey_1234567890!!!";
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

// ---------------------------
// 1. Add Controllers + JSON options
// ---------------------------
// We configure JSON so that:
//   - Enums are returned as strings, not numbers
//   - camelCase is used for properties (industry standard)
builder.Services.AddControllers()
  .AddJsonOptions(options =>
  {
      // Serialize enums as strings and ignore case when deserializing
      options.JsonSerializerOptions.Converters.Add(
    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
  );
  });

// ---------------------------
// 2. Add DbContext (Database setup)
// ---------------------------
// Uses connection string from appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



// ---------------------------
// 3. Register Repositories & Services (Dependency Injection)
// ---------------------------
// Scoped = one instance per request
// Repository handles DB, Service handles business logic
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserServices>();
// IJwtAuthManager now uses userId, username, and role
builder.Services.AddSingleton<IJwtAuthManager>(new JwtAuthManager(key));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // adjust as needed
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Authentication and Authorization MUST be before MapControllers
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
