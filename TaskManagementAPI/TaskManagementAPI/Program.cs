using TaskManagementAPI.Utilities;

var builder = WebApplication.CreateBuilder(args);
 
// ------------------------------------------------------
// 1. Register Services
// ------------------------------------------------------

builder.Services.AddHttpContextAccessor();
builder.Services.AddCustomControllers();
builder.Services.AddCorsPolicy();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAutoMapperConfig();
builder.Services.AddSwaggerWithJwt();

// ------------------------------------------------------
// 2. Build app
// ------------------------------------------------------

var app = builder.Build();

// ------------------------------------------------------
// 3. Middleware 
// ------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
