# Inter-Service Communication Writeup

## Overview

The Leave Management System uses a combination of **synchronous** (HTTP/REST) and **asynchronous** (RabbitMQ messaging) communication patterns between microservices.

---

## 1. Synchronous Communication (HTTP/REST)

### Client → API Gateway → Services

All client requests flow through the **Ocelot API Gateway** (port 5000), which routes to the appropriate downstream service based on URL path matching.

```
Client  →  API Gateway (5000)  →  Auth Service (5001)
                                →  Employee Service (5002)
                                →  Leave Service (5003)
                                →  Notification Service (5004)
```

### Leave Service → Employee Service (Internal)

When a leave request is submitted, the Leave Service makes **synchronous HTTP calls** to the Employee Service to:

1. **Validate leave balance** — Check if the employee has sufficient days remaining
2. **Deduct balance** — Reduce available days upon approval
3. **Release balance** — Restore days if leave is rejected/cancelled

```
Leave Service  ──HTTP PUT──►  Employee Service
               /api/employees/{id}/balance/{leaveType}/deduct
               /api/employees/{id}/balance/{leaveType}/release
```

**Resilience Policies (Polly):**
- **Retry**: 3 attempts with exponential backoff (300ms, 600ms, 900ms)
- **Circuit Breaker**: Opens after 5 consecutive failures, stays open for 30 seconds

---

## 2. Asynchronous Communication (RabbitMQ)

### Event-Driven Notifications

When leave status changes, the Leave Service **publishes events** to RabbitMQ. The Notification Service **consumes** these events asynchronously to create notification records.

```
Leave Service  ──publish──►  RabbitMQ  ──consume──►  Notification Service
```

### RabbitMQ Configuration

| Component | Value |
|-----------|-------|
| Exchange | `leave-events-exchange` |
| Exchange Type | Direct |
| Queue | `notification_queue` |
| Routing Keys | `leave.applied`, `leave.approved`, `leave.rejected`, `leave.cancelled` |

### Event Flow

1. **Leave Applied** → Notification sent to the employee's manager
2. **Leave Approved** → Notification sent to the employee
3. **Leave Rejected** → Notification sent to the employee (with rejection reason)
4. **Leave Cancelled** → Notification sent to both the manager and the employee

### Message Format

```json
{
  "leaveRequestId": "LR-20260603-001",
  "employeeId": "emp-001",
  "employeeName": "John Employee",
  "managerId": "mgr-001",
  "leaveType": "Casual",
  "startDate": "2026-06-15",
  "endDate": "2026-06-16",
  "reason": "Personal work",
  "status": "Approved",
  "comments": "Approved. Enjoy!",
  "timestamp": "2026-06-03T11:00:00Z"
}
```

---

## 3. Service Discovery (Consul)

All services register themselves with **HashiCorp Consul** on startup and deregister on shutdown. This enables:
- Dynamic service location (no hardcoded URLs in production)
- Health monitoring via Consul health checks
- Service catalog for observability

---

## 4. Assumptions

| # | Assumption | Rationale |
|---|-----------|-----------|
| 1 | **In-memory data storage** | No external database is used; data resets on restart. This simplifies deployment for the assignment scope. |
| 2 | **Single instance per service** | No load balancing between multiple instances of the same service. Consul supports it but not configured. |
| 3 | **No message persistence guarantee** | RabbitMQ is configured with default durability. If RabbitMQ restarts, unprocessed messages may be lost. |
| 4 | **JWT shared secret** | All services share the same HMAC-SHA256 signing key configured in `appsettings.json`. In production, a centralized identity provider (e.g., Keycloak) would be used. |
| 5 | **Synchronous balance check** | Leave Service calls Employee Service synchronously to validate balance. If Employee Service is down, leave applications will fail (mitigated by circuit breaker). |
| 6 | **No saga/compensation pattern** | If notification delivery fails after leave approval, the leave remains approved. Eventual consistency is acceptable for notifications. |
| 7 | **Gateway handles auth forwarding** | The API Gateway forwards the JWT token to downstream services; it does not terminate authentication itself. Each service validates the token independently. |
| 8 | **Docker networking** | Services communicate via Docker's internal DNS (service names as hostnames). In production, Consul DNS or a service mesh would be used. |
| 9 | **No HTTPS between services** | Internal service-to-service calls use HTTP. TLS termination would happen at the gateway/load balancer in production. |
| 10 | **Pre-seeded users** | Authentication uses hardcoded users. No user registration endpoint exists. |

---

## 5. Trade-offs

| Decision | Trade-off |
|----------|-----------|
| Direct Exchange over Topic | Simpler routing but less flexible for adding new event consumers |
| HTTP for balance validation | Strong consistency but tight coupling; alternative: event sourcing |
| In-memory stores | Fast development but no data persistence |
| Single queue for all events | Simpler setup but all notifications processed sequentially |

---

## 6. Communication Diagram

```
┌──────────┐         ┌──────────┐         ┌──────────┐
│  Client  │──HTTP──►│ Gateway  │──HTTP──►│  Auth    │
└──────────┘         │ (Ocelot) │         │ Service  │
                     └────┬─────┘         └──────────┘
                          │
              ┌───────────┼───────────┐
              │           │           │
              ▼           ▼           ▼
        ┌──────────┐ ┌──────────┐ ┌──────────────┐
        │ Employee │ │  Leave   │ │ Notification │
        │ Service  │ │ Service  │ │   Service    │
        └──────────┘ └────┬─────┘ └──────▲───────┘
              ▲            │              │
              │   HTTP     │   AMQP       │
              └────────────┘   (async)    │
                               │          │
                          ┌────▼──────────┘
                          │   RabbitMQ    │
                          └───────────────┘
```
