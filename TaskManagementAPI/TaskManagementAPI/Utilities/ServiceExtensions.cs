using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories.Implementations;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services;

namespace TaskManagementAPI.Utilities
{
    public static class ServiceExtensions
    {
        // Configure and add controllers
        public static void AddCustomControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase, allowIntegerValues: false)
                    );
                });
        }

        // Configure and add the database context
        public static void AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        }

        // Register repositories and services for dependency injection
        public static void AddRepositoriesAndServices(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserServices>();
        }

        // Configure and add JWT authentication
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            // Bind JwtSettings from appsettings.json
            services.Configure<JwtSettings>(config.GetSection("JwtSettings"));

            // Register JwtAuthManager using DI
            services.AddSingleton<IJwtAuthManager, JwtAuthManager>();

            // Retrieve JwtSettings to configure authentication
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>();
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

            services.AddAuthentication(options =>
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
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey)
                };
            });
        }
    }
}
