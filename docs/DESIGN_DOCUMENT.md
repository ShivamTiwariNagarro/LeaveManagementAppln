# Leave Management System - Microservices Design Document

## 1. System Overview

The Leave Management System is a distributed application built using **microservices architecture** with **.NET Core 8**. It enables employees to apply for leaves and managers to approve/reject them, with real-time notifications delivered asynchronously.

---

## 2. Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                            CLIENT APPLICATIONS                            │
│                    (Postman / Web Browser / Mobile App)                   │
└────────────────────────────────┬─────────────────────────────────────────┘
                                 │ HTTP/HTTPS
                                 ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                        API GATEWAY (Port 5000)                            │
│                              Ocelot                                       │
│  ┌─────────────┐  ┌──────────────────┐  ┌───────────────────────┐       │
│  │   Routing   │  │  Authentication  │  │  Service Discovery    │       │
│  └─────────────┘  └──────────────────┘  └───────────────────────┘       │
└────────┬──────────────────┬──────────────────────┬───────────────────────┘
         │                  │                      │
         ▼                  ▼                      ▼
┌─────────────┐    ┌──────────────┐       ┌──────────────┐
│    Auth     │    │   Employee   │       │    Leave     │
│   Service   │    │   Service    │       │   Service    │
│  (Port 5001)│    │  (Port 5002) │       │  (Port 5003) │
├─────────────┤    ├──────────────┤       ├──────────────┤
│ • Login     │    │ • Profiles   │       │ • Apply      │
│ • JWT Token │    │ • Leave      │       │ • Approve    │
│ • Validate  │    │   Balance    │       │ • Reject     │
│             │    │ • Department │       │ • Cancel     │
└─────────────┘    └──────────────┘       └──────┬───────┘
                                                  │
                                                  │ Publishes Events
                                                  ▼
                              ┌──────────────────────────────────┐
                              │         RabbitMQ (5672)           │
                              │    Exchange: leave-events-exchange│
                              │    Type: Direct                   │
                              ├──────────────────────────────────┤
                              │  Queues:                          │
                              │  • leave.applied                  │
                              │  • leave.approved                 │
                              │  • leave.rejected                 │
                              └──────────────┬───────────────────┘
                                             │ Consumes Events
                                             ▼
                              ┌──────────────────────────────────┐
                              │      Notification Service         │
                              │         (Port 5004)               │
                              ├──────────────────────────────────┤
                              │  • Stores notifications           │
                              │  • Mark read/unread               │
                              │  • User notification history      │
                              └──────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│                     CONSUL - Service Discovery (8500)                     │
│          All services register on startup & deregister on shutdown        │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Service Responsibilities

| Service | Port | Responsibility |
|---------|------|---------------|
| **API Gateway** | 5000 | Request routing, authentication forwarding, single entry point |
| **Auth Service** | 5001 | User authentication, JWT token generation & validation |
| **Employee Service** | 5002 | Employee profiles, leave balance management |
| **Leave Service** | 5003 | Leave request lifecycle (apply, approve, reject, cancel) |
| **Notification Service** | 5004 | Async notification delivery via RabbitMQ consumer |

---

## 4. Communication Patterns

### Synchronous (HTTP/REST)
- Client → API Gateway → Individual Services
- Leave Service → Employee Service (validate balance, deduct balance)

### Asynchronous (RabbitMQ)
- Leave Service **publishes** events when leave is applied/approved/rejected
- Notification Service **consumes** events and creates notification records

---

## 5. Cross-Cutting Concerns

| Concern | Implementation |
|---------|---------------|
| **Logging** | Serilog (structured, console + file sinks) |
| **Distributed Tracing** | X-Correlation-ID header propagation |
| **Authentication** | JWT Bearer tokens with role claims |
| **Authorization** | Role-based: Employee, Manager |
| **Global Exception Handling** | Custom middleware with proper HTTP status codes |
| **Circuit Breaker** | Polly (retry + circuit breaker on inter-service HTTP calls) |
| **Service Discovery** | HashiCorp Consul |
| **Health Checks** | `/health` endpoint on all services |

---

## 6. Data Storage

All services use **in-memory data stores** for simplicity:
- AuthService and EmployeeService pre-seed users/employees for authentication
- LeaveService and NotificationService start empty
- No external database dependency
- Data resets on service restart
- Designed for easy swap to SQL/NoSQL

---

## 7. Security

- **JWT Authentication**: HMAC-SHA256 signed tokens (1-hour expiry)
- **Role-Based Access Control**: Manager-only endpoints for approvals
- **Gateway Authentication**: All requests validated at gateway level
- **Input Validation**: FluentValidation on all request DTOs

---

## 8. Deployment

- **Containerization**: Docker (each service has its own Dockerfile)
- **Orchestration**: Docker Compose (7 containers total)
- **Health Checks**: Docker healthcheck on all services
- **Infrastructure**: Consul + RabbitMQ run as separate containers

---

## 9. Design Decisions

| Decision | Rationale |
|----------|-----------|
| Ocelot Gateway | Lightweight, .NET native, easy configuration |
| RabbitMQ over Kafka | Simpler for notification use case, direct exchange sufficient |
| In-memory storage | Assignment scope, no DB setup overhead |
| Shared library | Avoid code duplication for DTOs, middleware, utilities |
| Direct exchange | One-to-one routing for leave events to notification queue |

---

## 10. Scalability Considerations

- Each service can be independently scaled
- Stateless services (JWT, no session)
- Message broker decouples notification processing
- Consul enables dynamic service discovery for scaled instances
