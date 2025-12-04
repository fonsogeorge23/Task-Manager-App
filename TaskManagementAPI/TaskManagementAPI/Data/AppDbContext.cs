using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Models;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : DbContext(options)
    {
        private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;

        // -----------------------------
        // DBSets representing tables
        // -----------------------------
        public DbSet<TaskObject> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<TaskUser> TaskUsers { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<TaskVisibility> TaskVisibilities { get; set; }

        // Optional override for current user in background operations
        public int? OverrideUserId { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -----------------------------
            // TaskObject configurations
            // -----------------------------
            modelBuilder.Entity<TaskObject>()
                .Property(t => t.Status)
                .HasConversion<string>(); // store enum as string

            modelBuilder.Entity<TaskObject>()
                .Property(t => t.IsActive)
                .HasDefaultValue(true);

            // AssignedToUser -> Users
            // Restrict delete to avoid multiple cascade paths
            modelBuilder.Entity<TaskObject>()
                .HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Task -> Project
            modelBuilder.Entity<TaskObject>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // safe to cascade

            // Task -> Visibility
            modelBuilder.Entity<TaskObject>()
                .HasOne(t => t.Visibility)
                .WithMany(v => v.Tasks)
                .HasForeignKey(t => t.TaskVisibilityId)
                .OnDelete(DeleteBehavior.Cascade); // safe to cascade

            // -----------------------------
            // TaskComment configurations
            // -----------------------------
            modelBuilder.Entity<TaskComment>()
                .HasOne(tc => tc.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(tc => tc.TaskId)
                .OnDelete(DeleteBehavior.Cascade); // deleting a Task deletes comments

            modelBuilder.Entity<TaskComment>()
                .HasOne(tc => tc.Author)
                .WithMany()
                .HasForeignKey(tc => tc.AuthorId)
                .OnDelete(DeleteBehavior.Restrict); // prevent multiple cascade paths to Users

            // -----------------------------
            // User configurations
            // -----------------------------
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>(); // store enum as string

            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);
            // -----------------------------
            // ProjectMember Configuration
            // -----------------------------
            modelBuilder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMembers)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectMember>()
                .Property(pm => pm.ProjectRole)
                .HasConversion<string>()
                .IsRequired();

            // -----------------------------
            // TaskUser Configuration
            // -----------------------------
            modelBuilder.Entity<TaskUser>()
                .HasOne(tu => tu.Task)
                .WithMany(t => t.TaskUsers)
                .HasForeignKey(tu => tu.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskUser>()
                .HasOne(tu => tu.User)
                .WithMany(u => u.TaskUsers)
                .HasForeignKey(tu => tu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------------
            // AuditLog configurations
            // -----------------------------
            modelBuilder.Entity<AuditLog>();

            // -----------------------------
            // TaskVisibility configurations
            // -----------------------------
            modelBuilder.Entity<TaskVisibility>()
                .Property(tv => tv.Name)
                .IsRequired()
                .HasMaxLength(100);
        }

        // -----------------------------
        // Override SaveChangesAsync to populate audit fields
        // -----------------------------
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<Base>();
            var currentUserId = GetCurrentUserId(); // fallback to 0 if unauthenticated

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedBy = currentUserId;
                        entry.Entity.IsActive = true;
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedBy = currentUserId;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        // -----------------------------
        // Helper to get current user id
        // -----------------------------
        private int GetCurrentUserId()
        {
            if (OverrideUserId.HasValue && OverrideUserId.Value > 0)
                return OverrideUserId.Value;

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
