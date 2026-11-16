using Shared.Models;

namespace TaskService.Services
{
    public static class TaskAccess
    {
        public static bool CanRead(UserTask task, Guid userId)
        {
            return task.OwnerId == userId || task.AssignedToUserId == userId;
        }

        public static bool CanEdit(UserTask task, Guid userId)
        {
            return task.OwnerId == userId;
        }

        public static bool CanComment(UserTask task, Guid userId)
        {
            return CanRead(task, userId);
        }

        public static bool CanAssign(UserTask task, Guid userId)
        {
            return task.OwnerId == userId;
        }

        public static bool CanDelete(UserTask task, Guid userId)
        {
            return task.OwnerId == userId;
        }
    }
}
