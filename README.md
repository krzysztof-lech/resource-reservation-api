# Resource Reservation API
A REST API for booking shared resources (meeting rooms, equipment, desks, etc.) built with ASP.NET Core 10 and Entity Framework Core. 
Users can browse available resources and create reservations within a resource's operating hours, while administrators manage users, categories, and resources.
## Features
- JWT authentication with role-based authorization (`User` / `Admin`)
- Resource catalog with categories, availability windows, and allowed days of the week
- Reservation booking with automatic validation of:
  - resource availability
  - allowed days / operating hours
  - overlapping time slots
- Reservation lifecycle modeled as a state machine (`Pending → Confirmed → Cancelled`)
- Role-aware responses — regular users see a public view of reservations, admins see full details (including the reservation owner)
- User management with search, filtering, and pagination
- Request validation via FluentValidation
- Secure password storage using PBKDF2 (SHA-256, salted, 100k iterations, constant-time comparison)
- Interactive API documentation via Scalar (OpenAPI)
- Unit tests (controllers & services) and integration tests (in-memory SQLite + `WebApplicationFactory`)

## Tech Stack
|Layer          |Technology                                                  |
|---------------|------------------------------------------------------------|
|Framework      |ASP.NET Core 10 (Web API)                                   |
|ORM            |Entity Framework Core 10 (SQL Server)                       |
|Auth           |JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`)|
|Validation     |FluentValidation + SharpGrip AutoValidation                 |
|API docs       |Scalar.AspNetCore (OpenAPI UI)                              |
|Testing        |xUnit, Moq, FluentAssertions, EF Core InMemory, SQLite      |
## Project Structure
```
ResourceReservation.Api/
├── Controllers/        # AuthController, UsersController, CategoriesController,
│                        # ResourcesController, ReservationsController
├── Services/            # Business logic (AuthService, UserService, CategoryService,
│                        # ResourceService, ReservationService, TokenService)
├── Services/Interfaces/ # Service contracts
├── Data/                # AppDbContext
├── Models/              # Entities (User, Resource, Category, Reservation, ReservationStatus)
├── Dtos/                # Request/response DTOs + mapping extensions
├── Validators/          # FluentValidation rules
├── Security/            # JwtSettings, PasswordHasher
└── Program.cs

ResourceReservation.Tests/
├── Controllers/         # Controller unit tests (mocked services)
├── Services/            # Service unit tests (EF Core InMemory)
└── Integration/         # End-to-end tests (WebApplicationFactory + SQLite)
```

## Getting Started
### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB is fine for local development)

### Configuration
Connection string and JWT settings live in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ResourceReservationDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_A_STRONG_RANDOM_BASE64_32_PLUS_CHARS",
    "Issuer": "ResourceReservation.Api",
    "Audience": "ResourceReservation.Api",
    "ExpiryMinutes": 60
  }
}
```
Do not commit real secrets. The project has a `UserSecretsId` configured — for local development, set the JWT key via .NET User Secrets instead of editing `appsettings.json` directly:
```bash
dotnet user-secrets set "Jwt:Key" "your-strong-random-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```

### Running the API
```bash
# Apply EF Core migrations (creates the database)
dotnet ef database update

# Run the API
dotnet run --project ResourceReservation.Api
```

In development, interactive API docs are available via Scalar at `/scalar` (OpenAPI spec at /openapi/v1.json).

### Running the Tests
```bash
dotnet test
```

This runs both the unit test suite (controllers/services, mocked dependencies or EF Core InMemory) and the integration test suite (full HTTP pipeline against a SQLite in-memory database).

## Authorization

Obtain a token via `POST /api/auth/login`, then send it as a Bearer token on subsequent requests:
```
Authorization: Bearer <token>
```

New accounts are created via `POST /api/users` (public endpoint) and are assigned the `User` role by default. Promoting a user to `Admin` requires an existing admin to update the user's role.

## API Endpoints

### Auth

| Method          | Endpoint        | Auth | Description                 |
|----------------|------------------|------|------------------------------|
| POST           |`/api/auth/login` |-   |Log in, returns a JWT            |

### Users

| Method          | Endpoint | Auth | Description              |
|----------------|-----------|------|--------------------------|
| GET          |`/api/users` |Admin |Search users (filters: `q`, `role`, `createdAfter`, `createdBefore`, `page`, `pageSize`)  |
| GET          |`/api/users/{id}` |Admin | Get a user by id     |
| POST         |`/api/users`| - |Register a new user |
| PUT          |`/api/users/{id}`|Admin |Update a user (including role) |
| DELETE       |`/api/users/{id}`|Admin |Delete a user |

### Categories
| Method          | Endpoint | Auth | Description                 |
|----------------|-----------|------|----------------------------|
| GET          |`/api/categories` | - |List all categories |
| GET          |`/api/categories/{id}` | - |Get a category                    |
| POST         |`/api/categories`|Admin |Create a category |
| PUT          |`/api/categories/{id}`|Admin |Update a category |
| DELETE       |`/api/categories/{id}`|Admin |Delete a category |

### Resources
| Method         | Endpoint | Auth | Description                 |
|----------------|----------|------|-----------------------------|
| GET          |`/api/resources` | - |Search resources (filters: `q`, `categoryId`, `isAvailable`, `day`, `atTime`). Non-admins only ever see available resources. |
| GET          |`/api/resources/{id}` | - |Get a resource (unavailable resources are hidden from non-admins)                    |
| POST         |`/api/resources`|Admin |Create a resource |
| PUT          |`/api/resources/{id}`|Admin |Update a resource |
| DELETE       |`/api/resources/{id}`|Admin |Delete a resource |

### Reservations
| Method        | Endpoint               | Auth | Description      |
|---------------|------------------------|------|------------------|
| GET           |`/api/reservations`     |User* |Search reservations (filters: `userId`, `status`, `isPast`).|
| GET           |`/api/reservations/{id}`|User |Get a reservation by id          |
| GET           |`/api/reservations/user/my`|User |Get the current user's reservations |
| POST          |`/api/reservations`|User |Create a reservation |
| PUT           |`/api/reservations/{id}/cancel`|User |Cancel a reservation (owner or admin only) |

\* Any authenticated user can see all reservations. Admins get full details (including the reservation owner); regular users get an anonymized view.
## Business Rules
Creating a reservation is rejected (`400 Bad Request`) if:
- the resource doesn't exist or is currently unavailable,
- the requested day of week isn't in the resource's allowed days,
- the requested time falls outside the resource's operating hours,
- the time range overlaps an existing, non-cancelled reservation on the same resource.

Reservation status follows a strict state machine:
```
Pending ──► Confirmed ──► Cancelled
   └───────────────────────┘
```

Once a reservation is `Cancelled`, it cannot transition to any other state.

Cancelling a reservation is allowed for the reservation's owner or an admin; anyone else receives `403 Forbidden`.

Validation (via FluentValidation) enforces, among other rules:

- passwords: minimum 8 characters
- emails: valid format
- reservation start time: must be in the future; end time must be after start time
- resource operating hours: "available to" must be after "available from"