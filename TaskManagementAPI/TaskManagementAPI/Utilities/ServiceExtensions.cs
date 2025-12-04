using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TaskManagementAPI.Data;
using TaskManagementAPI.Mappings;
using TaskManagementAPI.Models;
using TaskManagementAPI.Repositories;
using TaskManagementAPI.Services;

namespace TaskManagementAPI.Utilities
{
    public static class ServiceExtensions
    {
        // ------------------------------------------------------
        // Controllers + JSON Enum Configuration
        // ------------------------------------------------------
        public static void AddCustomControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // KEEP enum names exactly as defined in code ("Admin", "PM", "User", "Guest")
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter()
                    );
                });
        }

        // ------------------------------------------------------
        // Database Context
        // ------------------------------------------------------
        public static void AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        }

        // ------------------------------------------------------
        // Repository Layer
        // ------------------------------------------------------
        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
        }

        // ------------------------------------------------------
        // Service Layer
        // ------------------------------------------------------
        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserServices>();
            services.AddScoped<ITaskService, TaskServices>();

            services.AddScoped<IAuthenticationService, AuthenticationService>(); 
            services.AddScoped<IPermissionService, PermissionService>();
        }

        // ------------------------------------------------------
        // AutoMapper Configuration
        // ------------------------------------------------------
        public static void AddAutoMapperConfig(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MappingProfile));
        }

        // ------------------------------------------------------
        // JWT Authentication
        // ------------------------------------------------------
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            // Load settings safely
            var jwtSection = config.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSection);

            var jwtSettings = jwtSection.Get<JwtSettings>()
                ?? throw new Exception("Missing 'JwtSettings' in appsettings.json");

            // Highest priority: Environment variable
            var secretFromEnv = Environment.GetEnvironmentVariable("JWT_SECRET");

            // Fallback to appsettings.json value
            var finalSecret = !string.IsNullOrWhiteSpace(secretFromEnv)
                                ? secretFromEnv
                                : jwtSettings.SecretKey;
            if (string.IsNullOrWhiteSpace(finalSecret))
                throw new Exception("JWT secret is missing. Set 'JWT_SECRET' environment variable or add it to appsettings.json.");


            var keyBytes = Encoding.UTF8.GetBytes(finalSecret);

            services.AddSingleton<IJwtAuthManager, JwtAuthManager>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                };
            });
        }

        // ------------------------------------------------------
        // Authorization Policies
        // ------------------------------------------------------
        public static void AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                AddUserPolicies(options);
                AddProjectPolicies(options);
                AddTaskPolicies(options);
                AddCommentPolicies(options);
            });
        }
        // ===============================================================
        // USER POLICIES
        // ===============================================================
        private static void AddUserPolicies(AuthorizationOptions options)
        {
            options.AddPolicy(ActionTitles.CreateUser, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(ActionTitles.UpdateUser, policy =>
                policy.RequireAssertion(context =>
                {
                    var currentUserId = context.User.FindFirst("id")?.Value;
                    var targetUserId = context.Resource?.ToString();

                    return context.User.IsInRole("Admin") ||
                           currentUserId == targetUserId;
                }));

            options.AddPolicy(ActionTitles.ViewUser, policy =>
                policy.RequireAssertion(context =>
                {
                    // Admin and PM can view any user
                    if (context.User.IsInRole("Admin") || context.User.IsInRole("PM"))
                        return true;

                    // Normal user: can only view self
                    var currentUserId = context.User.FindFirst("id")?.Value;
                    var targetUserId = context.Resource?.ToString();

                    return currentUserId == targetUserId;
                }));

            options.AddPolicy(ActionTitles.ViewAllProjectUsers, policy =>
                policy.RequireRole("Admin", "PM"));
        }

        // ===============================================================
        // PROJECT POLICIES
        // ===============================================================
        private static void AddProjectPolicies(AuthorizationOptions options)
        {
            options.AddPolicy(ActionTitles.CreateProject, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(ActionTitles.UpdateProject, policy =>
                policy.RequireRole("Admin", "PM"));

            options.AddPolicy(ActionTitles.ViewProject, policy =>
                policy.RequireAssertion(context =>
                {
                    var userId = context.User.FindFirst("id")?.Value;
                    var projectId = context.Resource?.ToString();

                    if (context.User.IsInRole("Admin") || context.User.IsInRole("PM") || context.User.IsInRole("User"))
                        return true;

                    //if (context.User.IsInRole("Guest"))
                    //{
                    //    return PermissionChecker.IsGuestAllowed(userId!, projectId!);
                    //}

                    return false;
                }));

            options.AddPolicy(ActionTitles.DeleteProject, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(ActionTitles.AddUserToProject, policy =>
                policy.RequireRole("Admin", "PM"));
        }

        // ===============================================================
        // TASK POLICIES
        // ===============================================================
        private static void AddTaskPolicies(AuthorizationOptions options)
        {
            options.AddPolicy(ActionTitles.CreateTask, policy =>
                policy.RequireRole("Admin", "PM", "User"));

            options.AddPolicy(ActionTitles.UpdateTask, policy =>
                policy.RequireRole("Admin", "PM", "User"));

            options.AddPolicy(ActionTitles.UpdateTaskStatus, policy =>
                policy.RequireRole("Admin", "PM", "User"));

            options.AddPolicy(ActionTitles.ViewTask, policy =>
                policy.RequireAssertion(context =>
                {
                    var userId = context.User.FindFirst("id")?.Value;
                    var taskId = context.Resource?.ToString();

                    if (context.User.IsInRole("Admin") || context.User.IsInRole("PM") || context.User.IsInRole("User"))
                        return true;

                    //if (context.User.IsInRole("Guest"))
                    //{
                    //    return PermissionChecker.IsGuestAllowedForTask(userId!, taskId!);
                    //}

                    return false;
                }));

            options.AddPolicy(ActionTitles.DeleteTask, policy =>
                policy.RequireRole("Admin", "PM"));
        }

        // ===============================================================
        // COMMENT POLICIES
        // ===============================================================
        private static void AddCommentPolicies(AuthorizationOptions options)
        {
            options.AddPolicy(ActionTitles.CreateComment, policy =>
                policy.RequireRole("Admin", "PM", "User", "Guest"));

            options.AddPolicy(ActionTitles.UpdateComment, policy =>
                policy.RequireRole("Admin", "PM", "User"));

            options.AddPolicy(ActionTitles.DeleteComment, policy =>
                policy.RequireRole("Admin", "PM"));

            options.AddPolicy(ActionTitles.ViewComment, policy =>
                policy.RequireRole("Admin", "PM", "User", "Guest"));
        }


        // ------------------------------------------------------
        // CORS Policy
        // ------------------------------------------------------
        public static void AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });
        }

        // ------------------------------------------------------
        // Swagger + JWT Configuration
        // ------------------------------------------------------
        public static void AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Task Management API",
                    Version = "v1"
                });

                var jwtScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter: Bearer {token}",
                    Reference = new OpenApiReference
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                };

                options.AddSecurityDefinition("Bearer", jwtScheme);

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtScheme, Array.Empty<string>() }
                });
            });
        }
    }
}
