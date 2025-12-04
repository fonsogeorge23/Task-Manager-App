using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;
using TaskManagementAPI.Utilities;

namespace TaskManagementAPI.Services
{
    public interface IPermissionService
    {
        // Project-level
        Task<bool> CanViewProject(User user, Project project);
        Task<bool> CanModifyProject(User user, Project project);

        // Task-level
        Task<bool> CanViewTask(User user, TaskObject task);
        Task<bool> CanModifyTask(User user, TaskObject task);
        Task<bool> CanChangeStatus(User user, TaskObject task);
        Task<bool> CanComment(User user, TaskObject task);
        Task<bool> CanAssignTask(User user, TaskObject task);

        // User-level
        bool CanCreateUser(User user);
        bool CanDeactivateUser(User adminUser, User targetUser);
        bool CanActivateUser(User adminUser, User targetUser);
    }
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _db;

        public PermissionService(AppDbContext db)
        {
            _db = db;
        }

        #region PROJECT PERMISSIONS
        public async Task<bool> CanViewProject(User user, Project project)
        {
            if (user == null || project == null) return false;

            // Admin always can
            if (user.Role == UserRole.Admin) return true;

            // System PM can view his project
            if (user.Role == UserRole.PM && project.ManagerId == user.Id) return true;

            // Check ProjectMembers mapping (Member or Viewer or ProjectAdmin)
            return await _db.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == project.Id && pm.UserId == user.Id && pm.IsActive);
        }

        public async Task<bool> CanModifyProject(User user, Project project)
        {
            if (user == null || project == null) return false;

            // Admin can modify 
            if (user.Role == UserRole.Admin) return true;

            // System PM can modify his project
            if (user.Role == UserRole.PM && project.ManagerId == user.Id) return true;

            // If user is ProjectAdmin in ProjectMembers
            return await _db.ProjectMembers.AnyAsync(pm =>
                pm.ProjectId == project.Id &&
                pm.UserId == user.Id &&
                pm.ProjectRole == ProjectRole.ProjectAdmin &&
                pm.IsActive);
        }
        #endregion

        #region TASK PERMISSIONS
        public async Task<bool> CanViewTask(User user, TaskObject task)
        {
            if (user == null || task == null) return false;

            // Admin / PM global
            if (user.Role == UserRole.Admin || user.Role == UserRole.PM) return true;

            // If user is assigned as primary assignee
            if (task.AssignedToUserId != 0 && task.AssignedToUserId == user.Id) return true;

            // If user is a TaskUser collaborator
            var isTaskUser = await _db.TaskUsers.AnyAsync(tu => tu.TaskId == task.Id && tu.UserId == user.Id && tu.IsActive);
            if (isTaskUser) return true;

            // If user is project member (Member or Viewer or ProjectAdmin)
            return await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == user.Id && pm.IsActive);
        }

        public async Task<bool> CanModifyTask(User user, TaskObject task)
        {
            if (user == null || task == null) return false;

            // Admin always can
            if (user.Role == UserRole.Admin) return true;

            // PM can modify tasks in projects where they are project admin or manager
            if (user.Role == UserRole.PM)
            {
                // manager of project?
                var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == task.ProjectId);
                if (project != null && project.ManagerId == user.Id) return true;

                // or PM as ProjectAdmin in ProjectMembers
                var isPmProjectAdmin = await _db.ProjectMembers.AnyAsync(pm =>
                    pm.ProjectId == task.ProjectId &&
                    pm.UserId == user.Id &&
                    pm.ProjectRole == ProjectRole.ProjectAdmin &&
                    pm.IsActive);
                if (isPmProjectAdmin) return true;
            }

            // Members may modify tasks if they are the primary assignee
            if (user.Role == UserRole.Member)
            {
                if (task.AssignedToUserId == user.Id) return true;

                // or if they are ProjectAdmin for that project
                var isProjectAdmin = await _db.ProjectMembers.AnyAsync(pm =>
                    pm.ProjectId == task.ProjectId &&
                    pm.UserId == user.Id &&
                    pm.ProjectRole == ProjectRole.ProjectAdmin &&
                    pm.IsActive);
                if (isProjectAdmin) return true;
            }

            return false;
        }

        public async Task<bool> CanChangeStatus(User user, TaskObject task)
        {
            // Allow same as modify, optionally allow assignee even if not modifier
            if (user == null || task == null) return false;

            // Assigned user can change status
            if (task.AssignedToUserId != 0 && task.AssignedToUserId == user.Id) return true;

            return await CanModifyTask(user, task);
        }

        public Task<bool> CanAssignTask(User user, TaskObject task)
        {
            // Only Admin and PM (who are project admin/manager) can assign
            if (user == null) return Task.FromResult(false);
            if (user.Role == UserRole.Admin) return Task.FromResult(true);
            if (user.Role == UserRole.PM)
            {
                // We'll check project membership/manager on call site if needed
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public async Task<bool> CanComment(User user, TaskObject task)
        {
            // Guests may comment only if they can view the task; members too
            return await CanViewTask(user, task);
        }
        #endregion

        #region USER PERMISSIONS
        public bool CanCreateUser(User user)
            => user != null && user.Role == UserRole.Admin;

        public bool CanDeactivateUser(User adminUser, User targetUser)
            => adminUser != null && adminUser.Role == UserRole.Admin && targetUser != null && targetUser.IsActive;

        public bool CanActivateUser(User adminUser, User targetUser)
            => adminUser != null && adminUser.Role == UserRole.Admin && targetUser != null && !targetUser.IsActive;
        #endregion
    }
}
