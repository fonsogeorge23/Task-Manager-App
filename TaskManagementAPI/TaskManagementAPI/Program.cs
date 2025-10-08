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

// ---------------------------
// 3. Register Repositories & Services (Dependency Injection)
// ---------------------------
// Scoped = one instance per request
// Repository handles DB, Service handles business logic
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserServices>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IJwtService, JwtServices>();
var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
