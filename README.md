# HubCinemaAPI (Backend for User & Admin)

<p align="center">
  <img src="https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white&style=flat" />
  <img src="https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=white&style=flat" />
  <img src="https://img.shields.io/badge/ASP.NET_Core-5C2D91?logo=dotnet&logoColor=white&style=flat" />
  <img src="https://img.shields.io/badge/SQL_Server-CC2927?logo=microsoftsqlserver&logoColor=white&style=flat" />
  <img src="https://img.shields.io/badge/Redis-DC382D?logo=redis&logoColor=white&style=flat" />
  <img src="https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white&style=flat" />
</p>

## Introduction

HubCinemaAPI is the central backend for the **HubCinema** movie ticketing system, supporting both the end-user and admin-side applications.
It provides REST APIs for authentication, ticket booking, showtime management, seating, billing, and overall content administration.

Other components of the system:

* **User Frontend:** [HubCinema-WebUser](https://github.com/Khanguyen2005/HubCinema-WebUser)
* **Admin Frontend:** [HubCinema-WebAdmin](https://github.com/Khanguyen2005/HubCinema-WebAdmin)
* **Automated Testing:** [sqa-testing-report](https://github.com/Khanguyen2005/sqa-testing-report)

## Tech Stack

| Category       | Technology / Library                   |
| -------------- | -------------------------------------- |
| Language       | C#                                     |
| Framework      | ASP.NET Core Web API (.NET 8)          |
| ORM / Database | Entity Framework Core (SQL Server)     |
| Cache          | Redis (StackExchange.Redis)            |
| Authentication | JWT Bearer Authentication              |
| API Docs       | Swagger / Swashbuckle                  |
| Email          | SMTP (EmailSettings)                   |
| DevOps         | Docker, Docker Compose, GitHub Actions |

## Architecture & Folder Structure

The project follows a multi-layered API architecture: **Controllers → Services → Data/Models**.

```
HubCinemaAPI/
├── Controllers/          # API endpoints (auth, booking, admin, schedule, ...)
├── Services/             # Business logic for users, booking, email, seats, ...
├── AdminServices/        # Admin logic (dashboard, user mgmt, invoice, ...)
├── Data/                 # DbContext and database configuration
├── Models/
│   ├── Entities/         # Database entity mappings
│   └── DTOs/             # Data Transfer Objects
├── Helpers/              # JWT, hashing, OTP, model binders
├── wwwroot/data/seat-layout/ # Seat layout data (JSON)
└── Program.cs            # Pipeline configuration & DI setup
```

## Getting Started

### Requirements

* .NET 8 SDK
* SQL Server
* Redis
* (Optional) Docker & Docker Compose

### Installation & Local Run

```bash
git clone https://github.com/Khanguyen2005/HubCinemaAPI.git
cd HubCinemaAPI
dotnet restore
```

### Environment Configuration

Update `appsettings.json` or use environment variables:

```bash
ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=cinema;User ID=sa;Password=YourStrongPassword;TrustServerCertificate=True"
Redis__ConnectionString="localhost:6379,abortConnect=False"
Jwt__Key="your_jwt_secret"
Jwt__Issuer="HUBCinemaAPI"
Jwt__Audience="hubcinema-client"
Jwt__ExpiresInMinutes=60
EmailSettings__SmtpServer="smtp.gmail.com"
EmailSettings__Port=587
EmailSettings__SenderName="HubCinema"
EmailSettings__SenderEmail="you@example.com"
EmailSettings__Username="you@example.com"
EmailSettings__Password="app_password"
EmailSettings__EnableSsl=true
```

### Run Application

```bash
dotnet run --project API_Project.csproj
```

Swagger UI: `http://localhost:5264/swagger`

### Run with Docker Compose

```bash
docker-compose up --build
```

Default API URL: `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

## Key Features

* JWT authentication & authorization: login, register, password recovery, OTP.
* User account management: profile, password update, email update.
* Public catalog: movies, cinemas, rooms, food/combos, banners, news.
* Showtime management: filtering schedules, creating showtimes, timeline view.
* Seat management: seat layout, seat types, temporary seat locking using Redis.
* Booking & billing: booking creation, seat status updates, invoice retrieval.
* Admin system: manage movies, cinemas, rooms, food, users, dashboard analytics.
* Health checks: Database, Redis, Email SMTP connectivity.

## Contributors

**Team Members:** Khá, Bắc, Khoa, Thành
