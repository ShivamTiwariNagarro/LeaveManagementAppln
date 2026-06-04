# Leave Management System - API Documentation

Complete API endpoint documentation with request/response samples.

**Base URL**: `http://localhost:5000` (via API Gateway)

---

## Authentication

All protected endpoints require the header:
```
Authorization: Bearer <JWT_TOKEN>
```

---

## 1. Auth Service (`/api/auth`)

### POST /api/auth/login

Authenticate a user and receive a JWT token.

**Request:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "employee1",
  "password": "password123"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": "emp-001",
    "username": "employee1",
    "fullName": "John Employee",
    "role": "Employee",
    "expiresAt": "2026-06-03T15:00:00Z"
  }
}
```

**Response (401 Unauthorized):**
```json
{
  "success": false,
  "error": "Invalid credentials"
}
```

---

### GET /api/auth/validate

Validate the current JWT token.

**Request:**
```http
GET /api/auth/validate
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Token is valid",
  "data": {
    "userId": "emp-001",
    "username": "employee1",
    "role": "Employee"
  }
}
```

---

## 2. Employee Service (`/api/employees`)

### GET /api/employees/me

Get the current authenticated user's profile.

**Request:**
```http
GET /api/employees/me
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "emp-001",
    "fullName": "John Employee",
    "email": "john@company.com",
    "department": "Engineering",
    "managerId": "mgr-001",
    "managerName": "Alice Manager",
    "role": "Employee",
    "joinDate": "2024-01-15"
  }
}
```

---

### GET /api/employees

Get all employees. **Manager only.**

**Request:**
```http
GET /api/employees
Authorization: Bearer <manager_token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "emp-001",
      "fullName": "John Employee",
      "email": "john@company.com",
      "department": "Engineering",
      "managerId": "mgr-001",
      "role": "Employee"
    }
  ]
}
```

---

### GET /api/employees/{id}

Get a specific employee by ID.

**Request:**
```http
GET /api/employees/emp-001
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "emp-001",
    "fullName": "John Employee",
    "email": "john@company.com",
    "department": "Engineering",
    "managerId": "mgr-001",
    "managerName": "Alice Manager",
    "role": "Employee",
    "joinDate": "2024-01-15"
  }
}
```

**Response (404 Not Found):**
```json
{
  "success": false,
  "error": "Employee not found"
}
```

---

### GET /api/employees/manager/{managerId}

Get all employees under a specific manager. **Manager only.**

**Request:**
```http
GET /api/employees/manager/mgr-001
Authorization: Bearer <manager_token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "emp-001",
      "fullName": "John Employee",
      "email": "john@company.com",
      "department": "Engineering",
      "managerId": "mgr-001",
      "role": "Employee"
    },
    {
      "id": "emp-002",
      "fullName": "Jane Employee",
      "email": "jane@company.com",
      "department": "Engineering",
      "managerId": "mgr-001",
      "role": "Employee"
    }
  ]
}
```

---

### GET /api/employees/me/balance

Get the current user's leave balance.

**Request:**
```http
GET /api/employees/me/balance?year=2026&leaveType=Casual
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "leaveType": "Casual",
      "year": 2026,
      "totalDays": 12,
      "usedDays": 2,
      "remainingDays": 10
    }
  ]
}
```

---

### GET /api/employees/{id}/balance

Get a specific employee's leave balance.

**Request:**
```http
GET /api/employees/emp-001/balance?year=2026
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "leaveType": "Casual",
      "year": 2026,
      "totalDays": 12,
      "usedDays": 2,
      "remainingDays": 10
    },
    {
      "leaveType": "Sick",
      "year": 2026,
      "totalDays": 10,
      "usedDays": 0,
      "remainingDays": 10
    },
    {
      "leaveType": "Privilege",
      "year": 2026,
      "totalDays": 15,
      "usedDays": 0,
      "remainingDays": 15
    }
  ]
}
```

---

## 3. Leave Service (`/api/leaves`)

### POST /api/leaves

Apply for a new leave request.

**Request:**
```http
POST /api/leaves
Content-Type: application/json
Authorization: Bearer <employee_token>

{
  "leaveType": "Casual",
  "startDate": "2026-06-15",
  "endDate": "2026-06-16",
  "reason": "Personal work to attend"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Leave request submitted successfully",
  "data": {
    "id": "LR-20260603-001",
    "employeeId": "emp-001",
    "employeeName": "John Employee",
    "leaveType": "Casual",
    "startDate": "2026-06-15",
    "endDate": "2026-06-16",
    "numberOfDays": 2,
    "reason": "Personal work to attend",
    "status": "Pending",
    "appliedOn": "2026-06-03T10:00:00Z"
  }
}
```

**Response (400 Bad Request - Overlap):**
```json
{
  "success": false,
  "error": "You already have an overlapping leave request (Pending/Approved) for the selected dates."
}
```

**Response (400 Bad Request - Insufficient Balance):**
```json
{
  "success": false,
  "error": "Insufficient leave balance. Available: 2 days, Requested: 5 days"
}
```

---

### GET /api/leaves/my

Get the current user's leave history with pagination.

**Request:**
```http
GET /api/leaves/my?page=1&pageSize=10
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "LR-20260603-001",
        "leaveType": "Casual",
        "startDate": "2026-06-15",
        "endDate": "2026-06-16",
        "numberOfDays": 2,
        "reason": "Personal work",
        "status": "Pending",
        "appliedOn": "2026-06-03T10:00:00Z"
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1
  }
}
```

---

### GET /api/leaves/{id}

Get a specific leave request by ID.

**Request:**
```http
GET /api/leaves/LR-20260603-001
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "LR-20260603-001",
    "employeeId": "emp-001",
    "employeeName": "John Employee",
    "leaveType": "Casual",
    "startDate": "2026-06-15",
    "endDate": "2026-06-16",
    "numberOfDays": 2,
    "reason": "Personal work",
    "status": "Pending",
    "appliedOn": "2026-06-03T10:00:00Z",
    "managerId": "mgr-001"
  }
}
```

---

### GET /api/leaves/pending

Get pending leave requests for approval. **Manager only.**

**Request:**
```http
GET /api/leaves/pending
Authorization: Bearer <manager_token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "LR-20260603-001",
      "employeeId": "emp-001",
      "employeeName": "John Employee",
      "leaveType": "Casual",
      "startDate": "2026-06-15",
      "endDate": "2026-06-16",
      "numberOfDays": 2,
      "reason": "Personal work",
      "status": "Pending",
      "appliedOn": "2026-06-03T10:00:00Z"
    }
  ]
}
```

---

### GET /api/leaves/team

Get all team leave requests with filters. **Manager only.**

**Request:**
```http
GET /api/leaves/team?status=Approved&employeeId=emp-001&startDate=2026-06-01&endDate=2026-06-30
Authorization: Bearer <manager_token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "LR-20260603-001",
      "employeeId": "emp-001",
      "employeeName": "John Employee",
      "leaveType": "Casual",
      "startDate": "2026-06-15",
      "endDate": "2026-06-16",
      "numberOfDays": 2,
      "status": "Approved",
      "approvedBy": "mgr-001",
      "approvedOn": "2026-06-03T11:00:00Z"
    }
  ]
}
```

---

### POST /api/leaves/{id}/approve

Approve a pending leave request. **Manager only.**

**Request:**
```http
POST /api/leaves/LR-20260603-001/approve
Content-Type: application/json
Authorization: Bearer <manager_token>

{
  "comments": "Approved. Enjoy your leave!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Leave request approved successfully",
  "data": {
    "id": "LR-20260603-001",
    "status": "Approved",
    "approvedBy": "mgr-001",
    "approvedOn": "2026-06-03T11:00:00Z",
    "comments": "Approved. Enjoy your leave!"
  }
}
```

---

### POST /api/leaves/{id}/reject

Reject a pending leave request. **Manager only.**

**Request:**
```http
POST /api/leaves/LR-20260603-001/reject
Content-Type: application/json
Authorization: Bearer <manager_token>

{
  "comments": "Team is short-staffed that week, please reschedule."
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Leave request rejected",
  "data": {
    "id": "LR-20260603-001",
    "status": "Rejected",
    "rejectedBy": "mgr-001",
    "rejectedOn": "2026-06-03T11:00:00Z",
    "comments": "Team is short-staffed that week, please reschedule."
  }
}
```

---

### POST /api/leaves/{id}/cancel

Cancel own pending leave request.

**Request:**
```http
POST /api/leaves/LR-20260603-001/cancel
Authorization: Bearer <employee_token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Leave request cancelled successfully"
}
```

---

## 4. Notification Service (`/api/notifications`)

### GET /api/notifications

Get all notifications for the current user.

**Request:**
```http
GET /api/notifications
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "notif-001",
      "userId": "emp-001",
      "title": "Leave Approved",
      "message": "Your Casual leave from 2026-06-15 to 2026-06-16 has been approved.",
      "type": "LeaveApproved",
      "isRead": false,
      "createdAt": "2026-06-03T11:00:00Z"
    }
  ]
}
```

---

### GET /api/notifications/unread

Get unread notifications count.

**Request:**
```http
GET /api/notifications/unread
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "unreadCount": 3,
    "notifications": [...]
  }
}
```

---

### POST /api/notifications/{id}/read

Mark a notification as read.

**Request:**
```http
POST /api/notifications/notif-001/read
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Notification marked as read"
}
```

---

### POST /api/notifications/read-all

Mark all notifications as read.

**Request:**
```http
POST /api/notifications/read-all
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "All notifications marked as read"
}
```

---

### GET /api/notifications/{id}

Get a specific notification by ID.

**Request:**
```http
GET /api/notifications/notif-001
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "notif-001",
    "userId": "emp-001",
    "title": "Leave Approved",
    "message": "Your Casual leave from 2026-06-15 to 2026-06-16 has been approved.",
    "type": "LeaveApproved",
    "isRead": false,
    "createdAt": "2026-06-03T11:00:00Z"
  }
}
```

**Response (404 Not Found):**
```json
{
  "success": false,
  "error": "Notification not found"
}
```

---

## 5. Health Check (All Services)

### GET /health

**Request:**
```http
GET /health
```

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "service": "LeaveService",
  "timestamp": "2026-06-03T10:00:00Z"
}
```

---

## Error Response Format

All errors follow a consistent format:

```json
{
  "success": false,
  "error": "Error description message",
  "code": "ERROR_CODE",
  "traceId": "00-abc123def456...-01"
}
```

| HTTP Code | Meaning |
|-----------|---------|
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (missing/invalid token) |
| 403 | Forbidden (insufficient role) |
| 404 | Not Found |
| 409 | Conflict (duplicate/overlap) |
| 500 | Internal Server Error |
