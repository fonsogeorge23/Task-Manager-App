using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Models;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : DbContext(options)
    {
        private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;

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

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();
            modelBuilder.Entity<User>().Property(u => u.IsActive)
                .HasDefaultValue(true);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<Base>();
            var currentUserId = GetCurrentUserId();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.UtcNow;
                        entry.Entity.CreatedBy = currentUserId;
                        entry.Entity.UpdatedDate = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = currentUserId;
                        entry.Entity.IsActive = true;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.UtcNow;
                        entry.Entity.UpdatedBy = currentUserId;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        private int GetCurrentUserId()
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                var user = httpContext?.User;
                return user?.GetTokenUserId() ?? 0;
            }
            catch
            {
                return 0; // fallback for unauthenticated operations
            }
        }
    }
}
