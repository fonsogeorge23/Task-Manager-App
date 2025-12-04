using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManagementAPI.Data
{
    /// <summary>
    /// Factory for creating AppDbContext instances at design time.
    /// Used by EF Core tools (Add-Migration, Update-Database).
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Build configuration manually since Program.cs is not executed at design time
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Retrieve connection string 
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configure EF Core to use SQL Server with the same connection string as runtime
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            // Return a new AppDbContext without IHttpContextAccessor
            return new AppDbContext(optionsBuilder.Options, null!);
        }
    }
}
