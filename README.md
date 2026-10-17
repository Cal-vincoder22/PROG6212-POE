# RaceDay Part 1 & 2

PROG6212 Programming 2B - Portfolio of Evidence - Part 1: System Planning and Database

## System description

RaceDay is a full-stack event management system for the South African road running,
walking, and cycling community. It allows Event Organisers to create and manage events,
categories, and participant results, while Participants can browse upcoming events, enter
events by selecting a category, and track their personal race history.

This repository currently contains **Part 1** of the Portfolio of Evidence: the planning
and database phase. No application code has been written yet — Part 1 covers the ERD,
the API endpoint plan, and the SQL database script only, as required by the brief.

## Roles

RaceDay supports two user roles, selected at registration:

- **Organiser** - can create, edit, and delete events, manage event categories, capture
  participant results, and view all enrolments for their events.
- **Participant** - can create an account, browse events, enter an event by selecting a
  category, view their own enrolments, and track their personal results.

## Repository structure

```
/docs
  RaceDay_ERD.png             - Entity Relationship Diagram
  RaceDay_API_Endpoint_Plan.md - API endpoint plan (Section B)
  RaceDay_Database_Script.sql  - SQL database creation and seed script (Section C)
README.md
```

## Setup instructions

1. Install SQL Server and SQL Server Management Studio (SSMS) if not already installed.
2. Open SSMS and connect to your local SQL Server instance.
3. Open `docs/RaceDay_Database_Script.sql` in SSMS.
4. Click **Execute** to run the script. This drops any existing `RaceDayDB` database,
   creates a new one, creates all tables with their constraints, and seeds it with sample
   data (2 Organisers, 2 Participants, 3 Events, categories, and sample enrolments).
5. Expand `RaceDayDB > Tables` in the Object Explorer to confirm all 6 tables were created:
   `Roles`, `Users`, `Events`, `Categories`, `Enrolments`, `Results`.
6. Run a `SELECT * FROM Users;` query to confirm the seed data loaded correctly.

## CI/CD

GitHub Actions validates that the `/docs` folder exists and contains the required files
(ERD, endpoint plan, SQL script) on every push.

**Screenshot of successful green build:**

[<img width="1485" height="235" alt="green" src="https://github.com/user-attachments/assets/7ef93f9d-09e7-4e43-bd10-507a1929b931" />
]

## Video presentation

An unlisted YouTube video walking through the planning documents, the ERD decisions, the
endpoint plan choices, and running the SQL script live in SSMS:

[https://youtu.be/JhJqo5YfLeg]

## AI usage disclosure

CLAUDE CODE (e.g. planning, proofreading, ERD/endpoint
     plan drafting)

---

# Part 2: RESTful API (RaceDay.Api)

ASP.NET Core 8 Web API using Entity Framework Core (Code-First) with SQL Server, session-based authentication, role-based authorisation (Organiser / Participant), Swagger documentation and xUnit tests.

## How to run

1. Install the .NET 8 SDK and SQL Server LocalDB (included with Visual Studio).
2. Clone the repo and open `RaceDay/RaceDay.slnx` in Visual Studio.
3. Check the connection string in `RaceDay/RaceDay.Api/appsettings.json`:
   `Server=(localdb)\MSSQLLocalDB;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;`
4. Open **Tools → NuGet Package Manager → Package Manager Console**, set the default project to `RaceDay.Api`, and run `Update-Database`. This creates `RaceDayDB` from the `InitialCreate` migration and seeds the two roles.
5. Press **F5**. Swagger opens at `https://localhost:7003/swagger`.

## Authentication and roles

- `POST /api/auth/register` creates a user as an `Organiser` or `Participant`. Passwords are hashed with BCrypt.
- `POST /api/auth/login` starts a server-side session storing the user ID and role. The session cookie is sent automatically by the browser (and by Swagger UI).
- Endpoints are protected by a custom `SessionAuthorize` filter: **401** if not logged in, **403** if the role is wrong.
- Organisers can only change their own events (ownership is checked, returning **403** otherwise).

## Endpoints

| Area | Method and route | Who | Success | Errors |
|---|---|---|---|---|
| Auth | `POST /api/auth/register` | Anyone | 201 | 400, 409 |
| Auth | `POST /api/auth/login` | Anyone | 200 | 401 |
| Auth | `POST /api/auth/logout` | Logged in | 200 | 401 |
| Profile | `GET /api/users/me` | Logged in | 200 | 401, 404 |
| Profile | `PUT /api/users/me` | Logged in | 200 | 400, 401, 404 |
| Events | `GET /api/events` (filters: `eventType`, `upcomingOnly`) | Anyone | 200 | |
| Events | `GET /api/events/{id}` | Anyone | 200 | 404 |
| Events | `POST /api/events` | Organiser | 201 | 400, 401, 403 |
| Events | `PUT /api/events/{id}` | Owning Organiser | 200 | 400, 401, 403, 404 |
| Events | `DELETE /api/events/{id}` | Owning Organiser | 204 | 401, 403, 404, 409 |
| Categories | `GET /api/events/{eventId}/categories` | Anyone | 200 | 404 |
| Categories | `POST /api/events/{eventId}/categories` | Owning Organiser | 201 | 400, 401, 403, 404 |
| Categories | `PUT /api/categories/{id}` | Owning Organiser | 200 | 400, 401, 403, 404 |
| Categories | `DELETE /api/categories/{id}` | Owning Organiser | 204 | 401, 403, 404, 409 |
| Enrolments | `POST /api/enrolments` | Participant | 201 | 400, 401, 403, 404, 409 |
| Enrolments | `GET /api/enrolments/mine` | Participant | 200 | 401, 403 |
| Enrolments | `GET /api/events/{eventId}/enrolments` | Owning Organiser | 200 | 401, 403, 404 |
| Results | `POST /api/enrolments/{enrolmentId}/result` | Owning Organiser | 201 | 400, 401, 403, 404, 409 |
| Results | `PUT /api/results/{id}` | Owning Organiser | 200 | 400, 401, 403, 404 |
| Results | `GET /api/results/mine` | Participant | 200 | 401, 403 |

## Business rules

- An email address can only be registered once (409).
- A Participant cannot enrol twice in the same category (409) or in an event that has already taken place (400).
- An event with enrolments cannot be deleted (409).
- A result can only be captured once per enrolment (409), never for a cancelled enrolment (400), and finish time must be greater than zero (400).

## Running the unit tests

1. In Visual Studio open **Test → Test Explorer** and click **Run All Tests**, or run `dotnet test RaceDay/RaceDay.Tests/RaceDay.Tests.csproj`.
2. The suite has **45 tests** using the EF Core in-memory provider and a fake session. They cover success and failure paths for Auth, Events, Enrolments, Results, Profile and the role-rejection filter (401 and 403).

## CI/CD

`.github/workflows/part2-build-test.yml` runs on every push and pull request to `main`. It restores, builds and runs all unit tests on Ubuntu with .NET 8. The Part 1 workflow still validates the `/docs` folder.

## Changes from the Part 1 plan

- `User` has an extra optional `ProfilePictureUrl` column, used by the profile endpoints.
- `Enrolment` stores `EventId` directly (as well as `CategoryId`) and a `Status` (Confirmed by default), which simplifies organiser queries and duplicate checks.
- Registration takes the role as text (`"Organiser"` or `"Participant"`) rather than a numeric ID.
  
## Video walkthrough

[https://youtu.be/ROM6_qx7lW0]
