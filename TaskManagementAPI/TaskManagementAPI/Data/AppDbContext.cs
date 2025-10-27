
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<TaskObject> Tasks { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskObject>()
                .Property(t => t.Status)
                .HasConversion<string>();
            modelBuilder.Entity<TaskObject>()
                .Property(t => t.IsActive)
                .HasDefaultValue(true);
            modelBuilder.Entity<TaskObject>().HasQueryFilter(t => t.IsActive);

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();
            modelBuilder.Entity<User>().Property(u => u.IsActive)
                .HasDefaultValue(true);
            modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive);
        }
    }
}
