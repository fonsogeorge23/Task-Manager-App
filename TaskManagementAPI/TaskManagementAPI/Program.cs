using TaskManagementAPI.Utilities;

var builder = WebApplication.CreateBuilder(args);

#region 1. Register Services
// Add Custom Controllers with JSON options
builder.Services.AddCustomControllers();

// Add Database context
builder.Services.AddDatabase(builder.Configuration);

// Add Repositories and Services
builder.Services.AddRepositoriesAndServices();

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add AutoMapper
builder.Services.AddEndpointsApiExplorer();     
builder.Services.AddSwaggerGen();              
var app = builder.Build();
#endregion

#region 2. Configure Middleware

//  Configure the HTTP request pipeline.
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

#endregion

await app.RunAsync();
