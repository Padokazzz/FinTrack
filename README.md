# FinTrack API

**FinTrack API** is a personal finance management REST API built with **C#**, **ASP.NET Core**, **Entity Framework Core**, **PostgreSQL**, **JWT Authentication**, **FluentValidation**, **xUnit** and a simplified **Clean Architecture** approach.

The goal of this project is to provide a clean, scalable and testable backend for managing accounts, categories, transactions and monthly financial summaries. It is designed as a portfolio project with real-world backend practices, clear architecture boundaries and professional documentation.

> Status: Work in progress. The project is being developed step by step, with the MVP features listed below.

## Table of Contents

- [About the Project](#about-the-project)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Running Locally](#running-locally)
- [Running with Docker](#running-with-docker)
- [Database Configuration](#database-configuration)
- [Migrations](#migrations)
- [Running Tests](#running-tests)
- [API Documentation](#api-documentation)
- [Main Endpoints](#main-endpoints)
- [Example Requests](#example-requests)
- [Security Notes](#security-notes)
- [Roadmap](#roadmap)
- [Git Workflow](#git-workflow)

## About the Project

FinTrack API helps users track their personal finances through a secure REST API.

Each authenticated user can manage:

- Financial accounts
- Income and expense categories
- Transactions
- Monthly summaries

The API is designed around a key security principle:

```text
A user can only access financial data that belongs to their own account.
```

This makes the project a strong portfolio piece because it demonstrates:

- REST API design
- Authentication and authorization with JWT
- Layered architecture
- Entity Framework Core persistence
- Validation with FluentValidation
- Unit testing
- Docker-based local environment
- Professional documentation

## Features

### MVP Scope

- User registration
- Login with JWT
- Financial account CRUD
- Category CRUD
- Transaction CRUD
- Transaction filters by month, year, type and category
- Monthly summary with:
  - Total income
  - Total expenses
  - Final balance
- Swagger documentation
- Unit tests
- Docker support

### Planned Improvements

- Refresh tokens
- Pagination for transaction lists
- Search transactions by description
- Annual reports
- Reports by category
- CSV import
- CSV export
- Integration tests
- GitHub Actions CI
- Cloud deployment

## Tech Stack

- **C#**
- **ASP.NET Core Web API**
- **Entity Framework Core**
- **PostgreSQL**
- **JWT Bearer Authentication**
- **Swagger / OpenAPI**
- **FluentValidation**
- **xUnit**
- **Moq**
- **FluentAssertions**
- **Docker**
- **Clean Architecture**

## Architecture

This project follows a simplified Clean Architecture structure.

```text
FinTrack.Api
FinTrack.Application
FinTrack.Domain
FinTrack.Infrastructure
FinTrack.Tests
```

### Layer Responsibilities

| Layer | Responsibility |
| --- | --- |
| `FinTrack.Domain` | Core business entities and enums |
| `FinTrack.Application` | DTOs, validators, interfaces and use cases |
| `FinTrack.Infrastructure` | Database, repositories, EF Core, JWT and technical implementations |
| `FinTrack.Api` | Controllers, middlewares, Swagger and dependency injection |
| `FinTrack.Tests` | Unit tests for application rules and validators |

### Dependency Flow

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application -> Domain
Infrastructure -> Domain
Tests -> Application
Tests -> Domain
```

The `Domain` layer does not depend on any other project.

## Project Structure

```text
fintrack-api/
|
├── src/
|   ├── FinTrack.Api/
|   |   ├── Controllers/
|   |   ├── Extensions/
|   |   ├── Middlewares/
|   |   ├── Program.cs
|   |   ├── appsettings.json
|   |   └── appsettings.Development.json
|   |
|   ├── FinTrack.Application/
|   |   ├── Common/
|   |   ├── DTOs/
|   |   ├── Interfaces/
|   |   ├── Services/
|   |   └── Validators/
|   |
|   ├── FinTrack.Domain/
|   |   ├── Common/
|   |   ├── Entities/
|   |   └── Enums/
|   |
|   └── FinTrack.Infrastructure/
|       ├── Authentication/
|       ├── Data/
|       ├── Extensions/
|       └── Repositories/
|
├── tests/
|   └── FinTrack.Tests/
|
├── Dockerfile
├── docker-compose.yml
├── FinTrack.slnx
├── .gitignore
└── README.md
```

## Getting Started

### Prerequisites

Install the following tools:

- .NET SDK 10 or newer
- PostgreSQL or Docker
- Git
- VS Code

Optional:

- GitHub CLI
- Docker Desktop
- Postman or Insomnia

Check your .NET version:

```bash
dotnet --version
```

## Running Locally

Clone the repository:

```bash
git clone https://github.com/YOUR_USERNAME/fintrack-api.git
cd fintrack-api
```

Restore dependencies:

```bash
dotnet restore
```

Build the projects:

```bash
dotnet build src/FinTrack.Domain/FinTrack.Domain.csproj
dotnet build src/FinTrack.Application/FinTrack.Application.csproj
dotnet build src/FinTrack.Infrastructure/FinTrack.Infrastructure.csproj
dotnet build src/FinTrack.Api/FinTrack.Api.csproj
dotnet build tests/FinTrack.Tests/FinTrack.Tests.csproj
```

Run the API:

```bash
dotnet run --project src/FinTrack.Api/FinTrack.Api.csproj
```

Open Swagger in your browser:

```text
https://localhost:{PORT}/swagger
```

or:

```text
http://localhost:{PORT}/swagger
```

The exact port is displayed in the terminal when the API starts.

## Running with Docker

This project is planned to run with Docker using:

- API container
- PostgreSQL container

After `Dockerfile` and `docker-compose.yml` are added, run:

```bash
docker compose up --build
```

Expected Swagger URL:

```text
http://localhost:8080/swagger
```

Stop containers:

```bash
docker compose down
```

Stop containers and remove volumes:

```bash
docker compose down -v
```

## Database Configuration

For local development, create your local settings file from the example:

```bash
cp src/FinTrack.Api/appsettings.Development.example.json src/FinTrack.Api/appsettings.Development.json
```

Then configure the connection string in:

```text
src/FinTrack.Api/appsettings.Development.json
```

Example for PostgreSQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fintrack_db;Username=YOUR_DATABASE_USER;Password=YOUR_DATABASE_PASSWORD"
  }
}
```

JWT settings example:

```json
{
  "Jwt": {
    "Issuer": "FinTrack.Api",
    "Audience": "FinTrack.Client",
    "Secret": "CHANGE_THIS_TO_A_LONG_SECURE_SECRET_KEY",
    "ExpirationMinutes": 60
  }
}
```

`appsettings.Development.json` is intentionally ignored by Git because it can contain local credentials. For production, do not store secrets directly in `appsettings.json`. Use environment variables or a secret manager.

## Migrations

Install the Entity Framework CLI if needed:

```bash
dotnet tool install --global dotnet-ef
```

Create a migration:

```bash
dotnet ef migrations add InitialCreate \
  --project src/FinTrack.Infrastructure \
  --startup-project src/FinTrack.Api \
  --output-dir Data/Migrations
```

Apply migrations:

```bash
dotnet ef database update \
  --project src/FinTrack.Infrastructure \
  --startup-project src/FinTrack.Api
```

## Running Tests

Run all tests:

```bash
dotnet test
```

Run only the test project:

```bash
dotnet test tests/FinTrack.Tests/FinTrack.Tests.csproj
```

Test coverage focus:

- Application services
- Validators
- Monthly summary calculations
- Transaction balance rules
- Authentication rules

## API Documentation

Swagger is available when running the API in development mode:

```text
/swagger
```

Swagger allows you to:

- Explore endpoints
- Test requests from the browser
- Authenticate using JWT
- Validate request and response formats

## Main Endpoints

### Authentication

```http
POST /api/auth/register
POST /api/auth/login
```

### Accounts

```http
GET    /api/accounts
GET    /api/accounts/{id}
POST   /api/accounts
PUT    /api/accounts/{id}
DELETE /api/accounts/{id}
```

### Categories

```http
GET    /api/categories
GET    /api/categories/{id}
POST   /api/categories
PUT    /api/categories/{id}
DELETE /api/categories/{id}
```

### Transactions

```http
GET    /api/transactions
GET    /api/transactions/{id}
POST   /api/transactions
PUT    /api/transactions/{id}
DELETE /api/transactions/{id}
```

Transaction filters:

```http
GET /api/transactions?month=6&year=2026&type=Expense&categoryId={categoryId}
```

### Monthly Summary

```http
GET /api/summaries/monthly?month=6&year=2026
```

## Example Requests

### Register

```http
POST /api/auth/register
Content-Type: application/json
```

```json
{
  "name": "Jane Doe",
  "email": "jane@example.com",
  "password": "123456"
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json
```

```json
{
  "email": "jane@example.com",
  "password": "123456"
}
```

Expected response:

```json
{
  "userId": "00000000-0000-0000-0000-000000000000",
  "name": "Jane Doe",
  "email": "jane@example.com",
  "token": "jwt-token-here"
}
```

Use the token in protected requests:

```http
Authorization: Bearer jwt-token-here
```

### Create Account

```http
POST /api/accounts
Authorization: Bearer jwt-token-here
Content-Type: application/json
```

```json
{
  "name": "Main Checking Account",
  "type": "Checking",
  "initialBalance": 1000
}
```

### Create Category

```http
POST /api/categories
Authorization: Bearer jwt-token-here
Content-Type: application/json
```

```json
{
  "name": "Salary",
  "type": "Income"
}
```

### Create Transaction

```http
POST /api/transactions
Authorization: Bearer jwt-token-here
Content-Type: application/json
```

```json
{
  "description": "June salary",
  "amount": 5000,
  "date": "2026-06-05T00:00:00Z",
  "type": "Income",
  "accountId": "account-guid-here",
  "categoryId": "category-guid-here"
}
```

### Monthly Summary Response

```json
{
  "month": 6,
  "year": 2026,
  "totalIncome": 5000,
  "totalExpense": 1800,
  "finalBalance": 3200
}
```

## Security Notes

- Passwords must be stored as hashes, never as plain text.
- JWT tokens are required for financial endpoints.
- `UserId` must come from the authenticated token, not from the request body.
- Repository queries must filter financial data by authenticated user.
- Secrets should be stored in environment variables outside local development.

## Roadmap

- [x] Create solution structure
- [x] Configure project references
- [x] Add base NuGet packages
- [x] Add domain entities
- [x] Add application DTOs
- [ ] Add FluentValidation validators
- [ ] Configure Entity Framework Core
- [ ] Configure PostgreSQL
- [ ] Implement repositories
- [ ] Implement services/use cases
- [ ] Configure JWT authentication
- [ ] Add controllers
- [ ] Protect endpoints by authenticated user
- [ ] Add migrations
- [ ] Add unit tests
- [ ] Add Docker support
- [ ] Add GitHub Actions CI

## Git Workflow

Recommended commit style:

```text
chore: create solution structure
feat: add domain entities
feat: add application dtos
feat: add request validators
chore: configure ef core
feat: add user registration
feat: add jwt login
feat: add account crud
feat: add category crud
feat: add transaction crud
feat: add monthly summary
test: add application unit tests
chore: add docker support
docs: update readme
```

## Author

Developed as a backend portfolio project focused on clean architecture, API design, authentication, persistence and testing with the .NET ecosystem.
