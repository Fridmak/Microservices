# Microservices

## To Start:
- Gateway:     `dotnet run --urls "http://localhost:7000"`
- Auth:        `dotnet run --urls "http://localhost:7001"`
- Task:        `dotnet run --urls "http://localhost:7002"`
- Notifications: `dotnet run --urls "http://localhost:7003"`

> SignalR client listens on Notifications service port.

## Available Collections in API:

### Priority
- Normal
- High
- Low
- Necessary

### NotificationType
- TaskCreated
- TaskAssigned
- TaskUpdated
- TaskDeleted
- TaskReassigned

### SortTasks
- ByPriority
- ByDeadline
- ByCreationDate
- Default

### UpdateCommentType (используется при обновлении комментариев к задачам)
- AddComment
- RemoveLastComment
