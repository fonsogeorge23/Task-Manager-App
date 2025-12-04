using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.Utilities
{
    public enum UserRole
    {
        Admin = 1,      // Super user, full access
        PM = 2,         // Project/Product manager, manages tasks/projects
        Member = 3,     // Regular user, can create tasks and update their status
        Guest = 4       // External viewer, limited access based on visibility
    }

    public enum ProjectRole
    {
        [Display(Name = "Project-Admin")]
        ProjectAdmin = 1,

        [Display(Name = "Project-Member")]
        ProjectMember = 2,

        [Display(Name = "Project-Viewer")]
        ProjectViewer = 3
    }
    public enum CurrentTaskStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Archived = 4
    }

    public enum PriorityLevel
    {
        High = 1,
        Medium = 2,
        Low = 3
    }
}
