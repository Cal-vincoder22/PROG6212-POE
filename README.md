# RaceDay

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
