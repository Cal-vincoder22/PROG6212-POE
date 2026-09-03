/*
    RaceDay - Database Creation Script
    PROG6212 Programming 2B - Part 1, Section C
    Run in SQL Server Management Studio (SSMS) on a clean SQL Server instance.
    Matches the Part 1 ERD and API Endpoint Plan exactly.
*/

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- ============================================================
-- TABLE: Roles
-- ============================================================
CREATE TABLE Roles (
    RoleId      INT             IDENTITY(1,1)   NOT NULL,
    RoleName    NVARCHAR(20)                    NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);
GO

-- ============================================================
-- TABLE: Users
-- ============================================================
CREATE TABLE Users (
    UserId          INT             IDENTITY(1,1)   NOT NULL,
    FullName        NVARCHAR(150)                   NOT NULL,
    Email           NVARCHAR(256)                   NOT NULL,
    PasswordHash    NVARCHAR(256)                   NOT NULL,
    RoleId          INT                             NOT NULL,
    PhoneNumber     NVARCHAR(20)                    NULL,
    ProfilePictureUrl NVARCHAR(500)                 NULL,
    CreatedAt       DATETIME2       DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

-- ============================================================
-- TABLE: Events
-- ============================================================
CREATE TABLE Events (
    EventId         INT             IDENTITY(1,1)   NOT NULL,
    OrganiserId     INT                             NOT NULL,
    Name            NVARCHAR(150)                   NOT NULL,
    Description     NVARCHAR(1000)                  NULL,
    EventDate       DATE                            NOT NULL,
    Location        NVARCHAR(200)                   NOT NULL,
    DistanceKm      DECIMAL(6,2)                    NOT NULL,
    EventType       NVARCHAR(20)                    NOT NULL,
    BannerImageUrl  NVARCHAR(500)                   NULL,
    CreatedAt       DATETIME2       DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CONSTRAINT CK_Events_DistanceKm CHECK (DistanceKm > 0)
);
GO

-- ============================================================
-- TABLE: Categories
-- ============================================================
CREATE TABLE Categories (
    CategoryId      INT             IDENTITY(1,1)   NOT NULL,
    EventId         INT                             NOT NULL,
    CategoryName    NVARCHAR(100)                   NOT NULL,
    MinAge          INT             NULL,
    MaxAge          INT             NULL,
    DistanceKm      DECIMAL(6,2)                    NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE,
    CONSTRAINT CK_Categories_Age CHECK (MaxAge IS NULL OR MinAge IS NULL OR MaxAge >= MinAge)
);
GO

-- ============================================================
-- TABLE: Enrolments
-- ============================================================
CREATE TABLE Enrolments (
    EnrolmentId     INT             IDENTITY(1,1)   NOT NULL,
    ParticipantId   INT                             NOT NULL,
    EventId         INT                             NOT NULL,
    CategoryId      INT                             NOT NULL,
    EnrolmentDate   DATETIME2       DEFAULT SYSUTCDATETIME() NOT NULL,
    Status          NVARCHAR(20)    DEFAULT 'Confirmed' NOT NULL,
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantId, EventId),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled'))
);
GO

-- ============================================================
-- TABLE: Results
-- ============================================================
CREATE TABLE Results (
    ResultId        INT             IDENTITY(1,1)   NOT NULL,
    EnrolmentId     INT                             NOT NULL,
    FinishTime      TIME(0)                         NOT NULL,
    FinishPosition  INT                             NOT NULL,
    CapturedAt      DATETIME2       DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId),
    CONSTRAINT UQ_Results_EnrolmentId UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_FinishPosition CHECK (FinishPosition > 0)
);
GO


-- ============================================================
-- SEED DATA
-- ============================================================

-- Roles
INSERT INTO Roles (RoleName) VALUES
    ('Organiser'),
    ('Participant');
GO

-- Users: 2 Organisers, 2 Participants
-- NOTE: PasswordHash values below are placeholders only, representing a hashed
-- password. Replace with real BCrypt/PBKDF2 hashes once the API is built in Part 2.
INSERT INTO Users (FullName, Email, PasswordHash, RoleId, PhoneNumber) VALUES
    ('Thabo Nkosi',   'thabo.nkosi@raceday.co.za',   'HASHED_PASSWORD_1', 1, '0821234567'),
    ('Amy van Wyk',   'amy.vanwyk@raceday.co.za',    'HASHED_PASSWORD_2', 1, '0837654321'),
    ('Sipho Dlamini', 'sipho.dlamini@example.com',   'HASHED_PASSWORD_3', 2, '0731122334'),
    ('Lerato Mokoena','lerato.mokoena@example.com',  'HASHED_PASSWORD_4', 2, '0764455667');
GO

-- Events: 3 Events, owned by the two Organisers
INSERT INTO Events (OrganiserId, Name, Description, EventDate, Location, DistanceKm, EventType) VALUES
    (1, 'Johannesburg City Run',   'An annual road running event through the streets of Johannesburg.', '2026-03-15', 'Johannesburg, Gauteng', 21.10, 'Run'),
    (1, 'Cape Town Cycle Classic', 'A scenic cycling event along the Cape Town coastline.',              '2026-04-12', 'Cape Town, Western Cape', 109.00, 'Cycle'),
    (2, 'Durban Beachfront Walk',  'A community charity walk along the Durban beachfront.',              '2026-05-03', 'Durban, KwaZulu-Natal', 5.00, 'Walk');
GO

-- Categories for each Event
INSERT INTO Categories (EventId, CategoryName, MinAge, MaxAge, DistanceKm) VALUES
    (1, '10km',      NULL, NULL, 10.00),
    (1, '21km',      NULL, NULL, 21.10),
    (2, '109km Full Ride', 19, NULL, 109.00),
    (2, '50km Half Ride',  15, NULL, 50.00),
    (3, 'Under 20',   NULL, 19, 5.00),
    (3, 'Senior',     20,  NULL, 5.00);
GO

-- Sample Enrolments
INSERT INTO Enrolments (ParticipantId, EventId, CategoryId, Status) VALUES
    (3, 1, 2, 'Confirmed'),
    (4, 1, 1, 'Confirmed'),
    (3, 3, 6, 'Pending'),
    (4, 2, 3, 'Confirmed');
GO

-- Sample Result (only for a completed enrolment)
INSERT INTO Results (EnrolmentId, FinishTime, FinishPosition) VALUES
    (1, '01:58:42', 47);
GO

SELECT * FROM Results;