# Leave Management System - Microservices

A complete Leave Management System built with .NET Core 8 microservices architecture.

---

## Submission Checklist

| # | Item | Link |
|---|------|------|
| 1 | Microservices Design Document | [docs/DESIGN_DOCUMENT.md](docs/DESIGN_DOCUMENT.md) |
| 2 | API Endpoint Documentation | [docs/API_DOCUMENTATION.md](docs/API_DOCUMENTATION.md) |
| 3 | Source Code | [GitHub Repository](https://github.com/ShivamTiwariNagarro/LeaveManagementAppln) |
| 4 | Inter-Service Communication Writeup | [docs/INTER_SERVICE_COMMUNICATION.md](docs/INTER_SERVICE_COMMUNICATION.md) |
| 5 | Dockerfile for each microservice | [src/Services/*/Dockerfile](src/) |
| 6 | Docker Hub Image Paths | Docker Hub blocked by IT team; using local build context in docker-compose.yml instead |
| 7 | docker-compose.yml | [src/docker-compose.yml](src/docker-compose.yml) |
| 8 | Demo Video/Recording | TODO: Add after recording |
| 9 | Postman Collection JSON | [LeaveManagementSystem.postman_collection.json](LeaveManagementSystem.postman_collection.json) |
| 10 | README.md | You are reading it! |

---

## Architecture

For detailed architecture diagram, design decisions, and service responsibilities, see:
- [Design Document](docs/DESIGN_DOCUMENT.md)

## Documentation

- [API Documentation](docs/API_DOCUMENTATION.md) - Complete request/response samples
- [Inter-Service Communication](docs/INTER_SERVICE_COMMUNICATION.md) - Communication patterns & assumptions
- [Postman Collection](LeaveManagementSystem.postman_collection.json) - Import into Postman for testing

---

## Setup Instructions

### Prerequisites
- .NET 8 SDK
- Docker & Docker Compose
- Postman (for API testing)

### How to Run with Docker Compose (Single Command)

```bash
cd src
docker-compose down -v ; docker-compose up --build -d
```

This single command starts **all 7 containers**:
- API Gateway (port 5000)
- Auth Service (port 5001)
- Employee Service (port 5002)
- Leave Service (port 5003)
- Notification Service (port 5004)
- RabbitMQ (port 5672 / Management UI: 15672)
- Consul (port 8500)

### Service URLs

| Service | URL |
|---------|-----|
| API Gateway | http://localhost:5000 |
| Auth Service | http://localhost:5001 |
| Employee Service | http://localhost:5002 |
| Leave Service | http://localhost:5003 |
| Notification Service | http://localhost:5004 |
| Consul Dashboard | http://localhost:8500 |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |

### Run Locally (Without Docker)

1. **Start infrastructure**:
   ```bash
   docker run -d --name consul -p 8500:8500 consul:1.15 agent -server -ui -bootstrap-expect=1 -client=0.0.0.0
   docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   ```

2. **Build and run**:
   ```bash
   cd src
   dotnet build

   # Run each service in separate terminals
   cd src/Services/AuthService && dotnet run
   cd src/Services/EmployeeService && dotnet run
   cd src/Services/LeaveService && dotnet run
   cd src/Services/NotificationService && dotnet run
   cd src/Gateway/ApiGateway && dotnet run
   ```

---

## Environment Variables

All configuration is in `appsettings.json` per service. Key settings:

| Variable | Default | Description |
|----------|---------|-------------|
| `Jwt:Key` | (in appsettings.json) | JWT signing secret key |
| `Jwt:Issuer` | `LeaveManagementSystem` | JWT token issuer |
| `Jwt:Audience` | `LeaveManagementAPI` | JWT token audience |
| `RabbitMQ:HostName` | `localhost` / `rabbitmq` (Docker) | RabbitMQ host |
| `RabbitMQ:Port` | `5672` | RabbitMQ port |
| `RabbitMQ:UserName` | `guest` | RabbitMQ username |
| `RabbitMQ:Password` | `guest` | RabbitMQ password |
| `Consul:Host` | `http://localhost:8500` | Consul server URL |
| `ServiceUrls:EmployeeService` | `http://localhost:5002` | Employee service URL (used by Leave Service) |

Docker Compose overrides these via `docker-compose.override.yml` for container networking.

---

## API Endpoints

### Authentication
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/auth/login` | POST | No | Login and get JWT token |
| `/api/auth/validate` | GET | Yes | Validate JWT token |

### Employees
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/employees/me` | GET | Yes | Get current user profile |
| `/api/employees/{id}` | GET | Yes | Get employee by ID |
| `/api/employees` | GET | Manager | Get all employees |
| `/api/employees/manager/{managerId}` | GET | Manager | Get employees by manager |
| `/api/employees/me/balance` | GET | Yes | Get own leave balance |
| `/api/employees/{id}/balance` | GET | Yes | Get employee's leave balance |

**Balance filters**: `?year=2026&leaveType=Casual`

### Leaves
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/leaves` | POST | Yes | Apply for leave |
| `/api/leaves/my` | GET | Yes | Get own leave requests |
| `/api/leaves/{id}` | GET | Yes | Get leave by ID |
| `/api/leaves/pending` | GET | Manager | Get pending approvals |
| `/api/leaves/team` | GET | Manager | Get all team leaves |
| `/api/leaves/{id}/approve` | POST | Manager | Approve leave |
| `/api/leaves/{id}/reject` | POST | Manager | Reject leave |
| `/api/leaves/{id}/cancel` | POST | Yes | Cancel own pending leave |

**My Leaves filters**: `?page=1&pageSize=10`
**Team Leaves filters**: `?status=Approved&employeeId=EMP001&startDate=2026-06-01&endDate=2026-06-30`

### Notifications
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/notifications` | GET | Yes | Get all notifications |
| `/api/notifications/unread` | GET | Yes | Get unread notifications |
| `/api/notifications/{id}` | GET | Yes | Get notification by ID |
| `/api/notifications/{id}/read` | POST | Yes | Mark as read |
| `/api/notifications/read-all` | POST | Yes | Mark all as read |

### Health Checks
| Endpoint | Service |
|----------|---------|
| `http://localhost:5000/health` | API Gateway |
| `http://localhost:5001/health` | Auth Service |
| `http://localhost:5002/health` | Employee Service |
| `http://localhost:5003/health` | Leave Service |
| `http://localhost:5004/health` | Notification Service |

---

## API Testing Instructions

### Using Postman Collection

1. Import `LeaveManagementSystem.postman_collection.json` into Postman
2. The collection includes:
   - All individual API endpoints organized by service
   - **End-to-End Success Flow** (Apply -> Approve -> Notification)
   - **End-to-End Rejection Flow** (Apply -> Reject -> Notification)
   - **End-to-End Cancellation Flow** (Apply -> Cancel)
   - Failure scenarios (invalid credentials, overlapping dates, unauthorized access)
3. Run the "End-to-End Flow" folders in sequence for complete workflow testing
4. Variables (`authToken`, `managerToken`, `leaveId`) are auto-populated via test scripts

### Quick Test with curl

```bash
# 1. Login as Employee
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"employee1","password":"password123"}'

# 2. Check Leave Balance (use token from step 1)
curl http://localhost:5000/api/employees/me/balance \
  -H "Authorization: Bearer YOUR_TOKEN"

# 3. Apply for Leave
curl -X POST http://localhost:5000/api/leaves \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"leaveType":"Casual","startDate":"2026-06-15","endDate":"2026-06-16","reason":"Personal work"}'

# 4. Login as Manager
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"manager1","password":"password123"}'

# 5. Approve Leave
curl -X POST http://localhost:5000/api/leaves/LEAVE_ID/approve \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer MANAGER_TOKEN" \
  -d '{"comments":"Approved. Enjoy your leave!"}'

# 6. Check Notifications
curl http://localhost:5000/api/notifications \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Pre-seeded Users

| Username | Password | Role | Manager |
|----------|----------|------|---------|
| manager1 | password123 | Manager | - |
| manager2 | password123 | Manager | - |
| employee1 | password123 | Employee | manager1 |
| employee2 | password123 | Employee | manager1 |
| employee3 | password123 | Employee | manager2 |
| employee4 | password123 | Employee | manager2 |

## Leave Allocation (Per Year)

| Leave Type | Days |
|------------|------|
| Casual | 12 |
| Sick | 10 |
| Privilege | 15 |

---

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET Core 8 (C#) |
| API Gateway | Ocelot |
| Service Discovery | Consul (HashiCorp) |
| Message Broker | RabbitMQ |
| Authentication | JWT Bearer (HMAC-SHA256) |
| Resilience | Polly (Circuit Breaker + Retry) |
| Logging | Serilog (Structured) |
| Distributed Tracing | X-Correlation-ID |
| Documentation | Swagger/OpenAPI |
| Containerization | Docker + Docker Compose |

---

## Project Structure

```
docs/
    DESIGN_DOCUMENT.md              # Architecture & design decisions
    API_DOCUMENTATION.md            # Complete API docs with samples
    INTER_SERVICE_COMMUNICATION.md  # Communication patterns & assumptions
src/
    Gateway/
        ApiGateway/                 # Ocelot API Gateway
    Services/
        AuthService/                # Authentication (JWT)
        EmployeeService/            # Employee & Leave Balance
        LeaveService/               # Leave Requests
        NotificationService/        # Notifications (RabbitMQ Consumer)
    Shared/
        Shared.Common/              # Shared DTOs, Middleware, Utilities
    docker-compose.yml              # All services + infrastructure
    LeaveManagement.sln
LeaveManagementSystem.postman_collection.json
README.md
```

---

## Troubleshooting

### Clean Start (Reset All Data)

Since all services use in-memory storage, a container restart clears service data. However, RabbitMQ uses a persistent volume. To do a full clean start:

```bash
cd src
docker-compose down -v          # Stop all containers AND remove volumes
docker-compose up --build -d    # Rebuild and start fresh
```

### Orphan RabbitMQ Queues

If you see unexpected queues in RabbitMQ Management UI (http://localhost:15672), reset only the RabbitMQ volume:

```bash
cd src
docker-compose stop rabbitmq
docker-compose rm -f rabbitmq
docker volume rm src_rabbitmq-data
docker-compose up -d rabbitmq
docker-compose restart notification-service leave-service
```

After reset, only `notification_queue` should exist with bindings for: `leave.applied`, `leave.approved`, `leave.rejected`, `leave.cancelled`.

### Consul Stale Service Registrations

If Consul shows red/critical services from previous runs, deregister them via:

```bash
# List services
curl http://localhost:8500/v1/agent/services

# Deregister a stale service
curl -X PUT http://localhost:8500/v1/agent/service/deregister/<service-id>
```

On fresh `docker-compose up`, services auto-register with fixed instance IDs and auto-deregister on shutdown.

### Services Not Connecting After RabbitMQ Restart

If NotificationService or LeaveService can't connect to RabbitMQ after a restart, simply restart them:

```bash
cd src
docker-compose restart notification-service leave-service
```

---

## Demo Video

> **Video Recording Link**: [Recording](https://nagarro-my.sharepoint.com/:v:/r/personal/shivam_tiwari_nagarro_com/Documents/ShivamTiwari_3163312_MicroserviceAssignment2026/ShivamTiwariMicroserviceAssignment.mp4?csf=1&web=1&e=GjWI7N)
>
> Duration: 10 minutes covering:
> - End-to-end success scenario (apply -> approve -> notification)
> - Failure scenarios (invalid login, overlapping dates, unauthorized access)
> - Cross-cutting concerns (logging, health checks, circuit breaker)

---





