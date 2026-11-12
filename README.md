# DevTaskHub API

Mobile app ke liye API list. Base URL:

`http://localhost:5187`

Phone ya emulator se `localhost` computer ka nahi hota.

- Android emulator: `http://10.0.2.2:5187`
- iOS simulator: `http://localhost:5187`
- Real phone: `http://<computer-lan-ip>:5187`

Swagger: `http://localhost:5187/swagger`

## Common rules

Har request:

```http
Content-Type: application/json
Authorization: Bearer <token>
```

Login, Register, aur Refresh-Token par token mat bhejo. Baaki sab par login se mila `token` bhejo.

JSON field names camelCase hain. Example: `fullName`, `projectId`.

Success response:

```json
{
  "success": true,
  "message": "...",
  "data": {}
}
```

Error response:

```json
{
  "statusCode": 400,
  "message": "The request is invalid.",
  "data": null,
  "errors": {
    "Email": ["Email is required."]
  }
}
```

`errors` sirf validation fail par aata hai.

Roles: `Admin`, `Project Manager`, `Developer`. Jahan Manager likha hai, Admin bhi call kar sakta hai.

Role ids:

| Role | Role id |
|---|---|
| Admin | `8f4c2a10-6b3e-4d91-9c27-1a5e7b0d4f21` |
| Project Manager | `3c9e1b74-2a68-4f05-b8d3-6e1c9a4d7b52` |
| Developer | `d17a6e83-5c42-4b19-a0f6-9b3d8e2c1a47` |

Enum values jo number mein bhejne hain:

| Field | Values |
|---|---|
| Task priority | `0` Low, `1` Medium, `2` High, `3` Critical |
| Task status | `0` ToDo, `1` InProgress, `2` InReview, `3` Done, `4` Blocked |
| Project status | string: `Active`, `OnHold`, `Completed`, `Archived` |

Password: kam se kam 8 characters, ek uppercase, ek lowercase, ek number, ek special character. Phone: exactly 10 digits.

## Auth

### POST `/api/Auth/Register`

No token. Naya user Developer ban kar register hota hai.

```json
{
  "fullName": "Rahul Sharma",
  "email": "rahul@example.com",
  "phoneNumber": "9876543210",
  "password": "Rahul@123"
}
```

### POST `/api/Auth/Login`

No token.

```json
{
  "email": "rahul@example.com",
  "password": "Rahul@123"
}
```

`data` mein `token`, `refreshToken`, `userId`, `fullName`, `email`, `phoneNumber`, `role`, `managerName` aata hai. `token` ko baaki APIs ke header mein rakho.

### POST `/api/Auth/Refresh-Token`

No token.

```json
{
  "refreshToken": "<refresh-token>"
}
```

### POST `/api/Auth/Chnage-Password`

Token required. URL spelling `Chnage` hai, `Change` nahi.

```json
{
  "currentPassword": "Rahul@123",
  "newPassword": "Rahul@456"
}
```

### GET `/api/Auth/Get-Profile-Details`

Token required. Body nahi.

`data`: `userId`, `fullName`, `email`, `phoneNumber`, `role`, `isActive`, `gitHubUserName`, `gitHubAvatarUrl`.

### POST `/api/Auth/Edit-Profile`

Token required.

```json
{
  "fullName": "Rahul Sharma",
  "phoneNumber": "9876543210"
}
```

### POST `/api/Auth/ActiveAndDeActiveUser-User`

Admin only. `active`: `1` activate, `0` deactivate.

```json
{
  "userId": "00000000-0000-0000-0000-000000000000",
  "active": 0
}
```

### POST `/api/Auth/Assign-Developer`

Admin only. Developer ko ek manager ke under assign karta hai.

```json
{
  "devloperID": "00000000-0000-0000-0000-000000000000",
  "managerID": "00000000-0000-0000-0000-000000000000"
}
```

### GET `/api/Auth/My-developers`

Admin aur Manager. Body nahi. Manager ko apne developers milte hain.

### GET `/api/Auth/Manager-Developers`

Admin only. Body nahi. Har manager ke saath uske developers aate hain.

## Roles

### GET `/api/RoleManage/Get-AllUser`

Admin only. Body nahi.

### GET `/api/RoleManage/Get-AllRole`

Admin only. Body nahi.

`data` item: `roleID`, `roleName`.

### POST `/api/RoleManage/Role-Assign`

Admin only.

```json
{
  "userID": "00000000-0000-0000-0000-000000000000",
  "roleID": "d17a6e83-5c42-4b19-a0f6-9b3d8e2c1a47"
}
```

## Projects

### POST `/api/Projects/Create-Project`

Admin aur Manager.

```json
{
  "projectName": "Employee Portal",
  "description": "Employee attendance and leave management"
}
```

`projectName` required, max 150. `description` max 1000.

### GET `/api/Projects/Get-All-Projects`

Admin, Manager, Developer.

Query parameters:

| Name | Default | Rule |
|---|---|---|
| `page` | `1` | 1 se kam ho to 1 |
| `pageSize` | `10` | 1 se 100 |
| `search` | empty | project name contains this text |
| `status` | empty | `Active`, `OnHold`, `Completed`, `Archived` |
| `sortBy` | `createdAt` | `name`, `status`, `createdAt` |
| `sortOrder` | `desc` | `asc` or `desc` |

Example: `/api/Projects/Get-All-Projects?page=1&pageSize=10&search=portal&status=Active&sortBy=name&sortOrder=asc`

`data`: `items`, `page`, `pageSize`, `totalCount`, `totalPages`.

Har item: `projectId`, `projectName`, `description`, `status`, `createdByUserId`, `createdByName`, `assignedToUserId`, `assignedToName`, `createdAt`, `updatedAt`.

Manager sirf apne assigned projects dekhta hai. Developer sirf un projects ko dekhta hai jinka member hai. Admin sab dekhta hai.

### PUT `/api/Projects/Edit-Project/{projectId}`

Admin aur Manager. `projectId` URL mein Guid hai.

```json
{
  "projectName": "Employee Portal",
  "description": "Updated description",
  "status": "Active"
}
```

`status`: `Active`, `OnHold`, `Completed`, `Archived`.

### DELETE `/api/Projects/Delete-Project/{projectId}`

Admin aur Manager. Body nahi.

### PUT `/api/Projects/Reassign-Manager-Project`

Admin only.

```json
{
  "projectID": "00000000-0000-0000-0000-000000000000",
  "assignedToUserID": "00000000-0000-0000-0000-000000000000"
}
```

`assignedToUserID` Admin ya Project Manager hona chahiye.

### POST `/api/Projects/Assign-Developer-To-Project`

Admin aur Manager.

```json
{
  "projectID": "00000000-0000-0000-0000-000000000000",
  "developerIDs": [
    "00000000-0000-0000-0000-000000000000"
  ]
}
```

`developerIDs` kam se kam 1 id.

### PUT `/api/Projects/Update-Project-Developers`

Admin aur Manager. Body `Assign-Developer-To-Project` jaisa hi hai.

### GET `/api/Projects/Get-Project-Developers/{projectId}`

Admin, Manager, Developer. Body nahi.

`data` item: `userId`, `fullName`, `gitHubAvatarUrl`.

## Tasks

### POST `/api/Task/Create-Task`

Admin aur Manager.

```json
{
  "projectID": "00000000-0000-0000-0000-000000000000",
  "title": "Login screen",
  "description": "Build the mobile login screen",
  "priority": 1,
  "dueDate": "2026-11-20T00:00:00Z"
}
```

`priority`: `0` Low, `1` Medium, `2` High, `3` Critical. `title` max 100. `description` max 1000.

### POST `/api/Task/Get-All-Tasks`

Admin, Manager, Developer. Yeh GET nahi hai, POST hai. Body nahi. Filters query string par hain.

Example: `/api/Task/Get-All-Tasks?search=login&status=1&priority=2&projectId=<guid>&sortBy=dueDate&sortOrder=asc`

| Name | Default | Rule |
|---|---|---|
| `search` | empty | task title contains this text |
| `status` | empty | `0` ToDo, `1` InProgress, `2` InReview, `3` Done, `4` Blocked. Name bhi chalega, jaise `InProgress` |
| `priority` | empty | `0` Low, `1` Medium, `2` High, `3` Critical |
| `projectId` | empty | sirf us project ke tasks |
| `sortBy` | `createdAt` | `title`, `status`, `priority`, `dueDate`, `createdAt` |
| `sortOrder` | `desc` | `asc` or `desc` |

`data` item: `taskID`, `projectID`, `projectName`, `title`, `taskSummary`, `status`, `priority`, `assignedToUserId`, `assignedToName`, `createdByUserId`, `createdByName`, `dueDate`, `createdAt`, `gitHubPRNumber`, `gitHubPRStatus`, `gitHubPRUrl`.

### PUT `/api/Task/Edit-Task/{taskID}`

Admin aur Manager.

```json
{
  "title": "Login screen",
  "description": "Updated summary",
  "priority": 2,
  "dueDate": "2026-11-22T00:00:00Z"
}
```

### POST `/api/Task/Assign-Task`

Admin aur Manager.

```json
{
  "taskID": "00000000-0000-0000-0000-000000000000",
  "developerID": "00000000-0000-0000-0000-000000000000"
}
```

### PUT `/api/Task/Update-Task-Status/{taskID}`

Admin, Manager, Developer.

```json
{
  "status": 1
}
```

`status`: `0` ToDo, `1` InProgress, `2` InReview, `3` Done, `4` Blocked.

## Dashboard

### GET `/api/Dashboard/Get-Dashboard`

Admin, Manager, Developer. Body nahi.

`data`: `totalProjects`, `totalTasks`, `toDoTasks`, `inProgressTasks`, `inReviewTasks`, `doneTasks`, `blockedTasks`. Admin response mein user, manager, aur developer counts bhi aa sakte hain.

## GitHub

### GET `/api/github/Get-Profile/{username}`

Admin, Manager, Developer. URL parameter `username`. Body nahi. GitHub account preview, save nahi hota.

### POST `/api/github/Link-Profile`

Admin, Manager, Developer. Logged-in user se link hota hai.

```json
{
  "username": "octocat"
}
```

### GET `/api/github/My-Profile`

Admin, Manager, Developer. Body nahi. Saved GitHub profile.

`data`: `gitHubId`, `username`, `profileUrl`, `avatarUrl`.

### POST `/api/github/Verify-Profile`

Admin, Manager, Developer. Body nahi. Saved username ko GitHub par dubara check karke photo aur URL refresh karta hai.

### DELETE `/api/github/Unlink-Profile`

Admin, Manager, Developer. Body nahi.

### POST `/api/github/Link-Repository`

Admin aur Manager.

```json
{
  "projectId": "00000000-0000-0000-0000-000000000000",
  "owner": "octocat",
  "repositoryName": "Hello-World"
}
```

### GET `/api/github/Repository/{projectId}`

Admin, Manager, Developer. Body nahi.

`data`: `projectId`, `owner`, `repositoryName`, `description`, `url`.

### DELETE `/api/github/Repository/{projectId}`

Admin aur Manager. Body nahi. Sirf repo link hatata hai, tasks nahi.

### POST `/api/github/Sync-PullRequests/{projectId}`

Admin, Manager, Developer. Body nahi. Latest 50 pull requests tasks mein save karta hai.

`data`: `created`, `updated`, `pullRequests`.

### GET `/api/github/PullRequests/{projectId}`

Admin, Manager, Developer. Body nahi. Us project ki saved pull requests.

Item: `taskId`, `number`, `title`, `status`, `url`, `lastSyncedAt`, `assignedToUserId`, `assignedToName`.

`status`: `Open`, `Merged`, `Closed`.

### GET `/api/github/Live-PullRequests`

Admin, Manager, Developer. Body nahi. Logged-in user ke allowed projects ki saved pull requests.
