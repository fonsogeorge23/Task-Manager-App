using Microsoft.OpenApi.Models;
using TaskManagementAPI.Utilities;

var builder = WebApplication.CreateBuilder(args);

#region 1. Register Services
// Add Custom Controllers with JSON options
builder.Services.AddCustomControllers();

// Register HttpContext accessor
builder.Services.AddHttpContextAccessor();

// Add Database context
builder.Services.AddDatabase(builder.Configuration);

// Add Repositories and Services
builder.Services.AddRepositoriesAndServices();

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add AutoMapper
builder.Services.AddEndpointsApiExplorer();

// Add Swagger + JWT Configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Management API",
        Version = "v1",
        Description = "A secure API for managing users and tasks with JWT authentication"
    });

    // Add JWT Bearer definition
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter 'Bearer' followed by your JWT token. Example: **Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9**",

        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);

    //Require token for all endpoints (optional but recommended)
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            jwtSecurityScheme,
            Array.Empty<string>()
        }
    });
});
#endregion

var app = builder.Build();

#region 2. Configure Middleware

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

//Important: Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

#endregion

await app.RunAsync();
