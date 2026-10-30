-- =========================================================================
-- STAYBNB - ASP.NET Core MVC Monolithic Application Database Script
-- University Module: Programming 742 (PRO 742)
-- Architecture: ASP.NET Core 8.0, EF Core 8.0, Microsoft SQL Server
-- =========================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'StaybnbDb')
BEGIN
    CREATE DATABASE StaybnbDb;
END
GO

USE StaybnbDb;
GO

-- 1. Identity Roles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetRoles')
BEGIN
    CREATE TABLE [dbo].[AspNetRoles] (
        [Id]               NVARCHAR(450) NOT NULL PRIMARY KEY,
        [Name]             NVARCHAR(256) NULL,
        [NormalizedName]   NVARCHAR(256) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL
    );
END
GO

-- 2. Identity Users (ApplicationUser) Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    CREATE TABLE [dbo].[AspNetUsers] (
        [Id]                   NVARCHAR(450) NOT NULL PRIMARY KEY,
        [UserName]             NVARCHAR(256) NULL,
        [NormalizedUserName]   NVARCHAR(256) NULL,
        [Email]                NVARCHAR(256) NULL,
        [NormalizedEmail]      NVARCHAR(256) NULL,
        [EmailConfirmed]       BIT NOT NULL,
        [PasswordHash]         NVARCHAR(MAX) NULL,
        [SecurityStamp]        NVARCHAR(MAX) NULL,
        [ConcurrencyStamp]     NVARCHAR(MAX) NULL,
        [PhoneNumber]          NVARCHAR(MAX) NULL,
        [PhoneNumberConfirmed] BIT NOT NULL,
        [TwoFactorEnabled]     BIT NOT NULL,
        [LockoutEnd]           DATETIMEOFFSET NULL,
        [LockoutEnabled]       BIT NOT NULL,
        [AccessFailedCount]    INT NOT NULL,
        [FirstName]            NVARCHAR(50) NOT NULL,
        [LastName]             NVARCHAR(50) NOT NULL,
        [ProfilePictureUrl]    NVARCHAR(255) NULL,
        [CreatedAt]            DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 3. Identity User Roles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUserRoles')
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles] (
        [UserId] NVARCHAR(450) NOT NULL,
        [RoleId] NVARCHAR(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END
GO

-- 4. HostProperty Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HostProperties')
BEGIN
    CREATE TABLE [dbo].[HostProperties] (
        [Id]            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Title]         NVARCHAR(100) NOT NULL,
        [Description]   NVARCHAR(MAX) NOT NULL,
        [PricePerNight] DECIMAL(18,2) NOT NULL,
        [HostId]        NVARCHAR(450) NOT NULL,
        [Address]       NVARCHAR(255) NOT NULL,
        [IsActive]      BIT NOT NULL DEFAULT 0,
        CONSTRAINT [FK_HostProperties_AspNetUsers] FOREIGN KEY ([HostId]) REFERENCES [dbo].[AspNetUsers] ([Id])
    );
END
GO

-- 5. Bookings Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Bookings')
BEGIN
    CREATE TABLE [dbo].[Bookings] (
        [Id]           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PropertyId]   INT NOT NULL,
        [GuestId]      NVARCHAR(450) NOT NULL,
        [CheckInDate]  DATETIME2 NOT NULL,
        [CheckOutDate] DATETIME2 NOT NULL,
        [TotalPrice]   DECIMAL(18,2) NOT NULL,
        [Status]       INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Approved, 2: Rejected, 3: Cancelled, 4: CheckedIn, 5: Completed
        CONSTRAINT [FK_Bookings_HostProperties] FOREIGN KEY ([PropertyId]) REFERENCES [dbo].[HostProperties] ([Id]),
        CONSTRAINT [FK_Bookings_AspNetUsers] FOREIGN KEY ([GuestId]) REFERENCES [dbo].[AspNetUsers] ([Id])
    );
END
GO

-- 6. Payments Table (1:1 with Booking)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
BEGIN
    CREATE TABLE [dbo].[Payments] (
        [Id]        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BookingId] INT NOT NULL UNIQUE,
        [Amount]    DECIMAL(18,2) NOT NULL,
        [Status]    INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Completed, 2: Refunded
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_Payments_Bookings] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings] ([Id]) ON DELETE CASCADE
    );
END
GO

-- 7. Reviews Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Reviews')
BEGIN
    CREATE TABLE [dbo].[Reviews] (
        [Id]         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PropertyId] INT NOT NULL,
        [ReviewerId] NVARCHAR(450) NOT NULL,
        [Rating]     INT NOT NULL CHECK ([Rating] BETWEEN 1 AND 5),
        [Comment]    NVARCHAR(1000) NOT NULL,
        [CreatedAt]  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_Reviews_HostProperties] FOREIGN KEY ([PropertyId]) REFERENCES [dbo].[HostProperties] ([Id]),
        CONSTRAINT [FK_Reviews_AspNetUsers] FOREIGN KEY ([ReviewerId]) REFERENCES [dbo].[AspNetUsers] ([Id])
    );
END
GO

-- 8. Amenities Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Amenities')
BEGIN
    CREATE TABLE [dbo].[Amenities] (
        [Id]        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name]      NVARCHAR(50) NOT NULL,
        [IconClass] NVARCHAR(50) NOT NULL
    );
END
GO

-- Many-to-Many Property Amenities Join Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PropertyAmenities')
BEGIN
    CREATE TABLE [dbo].[PropertyAmenities] (
        [AmenitiesId]  INT NOT NULL,
        [PropertiesId] INT NOT NULL,
        CONSTRAINT [PK_PropertyAmenities] PRIMARY KEY ([AmenitiesId], [PropertiesId]),
        CONSTRAINT [FK_PropertyAmenities_Amenities] FOREIGN KEY ([AmenitiesId]) REFERENCES [dbo].[Amenities] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PropertyAmenities_HostProperties] FOREIGN KEY ([PropertiesId]) REFERENCES [dbo].[HostProperties] ([Id]) ON DELETE CASCADE
    );
END
GO

-- 9. PropertyImages Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PropertyImages')
BEGIN
    CREATE TABLE [dbo].[PropertyImages] (
        [Id]         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PropertyId] INT NOT NULL,
        [ImageUrl]   NVARCHAR(500) NOT NULL,
        CONSTRAINT [FK_PropertyImages_HostProperties] FOREIGN KEY ([PropertyId]) REFERENCES [dbo].[HostProperties] ([Id]) ON DELETE CASCADE
    );
END
GO

-- 10. WishlistItems Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WishlistItems')
BEGIN
    CREATE TABLE [dbo].[WishlistItems] (
        [Id]         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId]     NVARCHAR(450) NOT NULL,
        [PropertyId] INT NOT NULL,
        [CreatedAt]  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_WishlistItems_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
        CONSTRAINT [FK_WishlistItems_HostProperties] FOREIGN KEY ([PropertyId]) REFERENCES [dbo].[HostProperties] ([Id]) ON DELETE CASCADE
    );
END
GO

-- 11. Messages Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Messages')
BEGIN
    CREATE TABLE [dbo].[Messages] (
        [Id]         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SenderId]   NVARCHAR(450) NOT NULL,
        [ReceiverId] NVARCHAR(450) NOT NULL,
        [Content]    NVARCHAR(MAX) NOT NULL,
        [Timestamp]  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_Messages_Sender] FOREIGN KEY ([SenderId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
        CONSTRAINT [FK_Messages_Receiver] FOREIGN KEY ([ReceiverId]) REFERENCES [dbo].[AspNetUsers] ([Id])
    );
END
GO

-- 12. ActivityLogs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ActivityLogs')
BEGIN
    CREATE TABLE [dbo].[ActivityLogs] (
        [Id]           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId]       NVARCHAR(450) NOT NULL,
        [Action]       NVARCHAR(100) NOT NULL,
        [ActivityType] NVARCHAR(100) NOT NULL,
        [CreatedAt]    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_ActivityLogs_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id])
    );
END
GO

-- 13. Notifications Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id]        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId]    NVARCHAR(450) NOT NULL,
        [Title]     NVARCHAR(100) NOT NULL,
        [Type]      NVARCHAR(100) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [IsRead]    BIT NOT NULL DEFAULT 0,
        CONSTRAINT [FK_Notifications_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id])
    );
END
GO

-- 14. HostApplications Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HostApplications')
BEGIN
    CREATE TABLE [dbo].[HostApplications] (
        [Id]                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ApplicationUserId] NVARCHAR(450) NOT NULL,
        [PropertyId]        INT NOT NULL,
        [Status]            INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Approved, 2: Rejected
        [AppliedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [AdminNotes]        NVARCHAR(500) NULL,
        CONSTRAINT [FK_HostApplications_AspNetUsers] FOREIGN KEY ([ApplicationUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]),
        CONSTRAINT [FK_HostApplications_HostProperties] FOREIGN KEY ([PropertyId]) REFERENCES [dbo].[HostProperties] ([Id])
    );
END
GO

-- 15. CheckInProcesses Table (1:1 with HostProperty)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CheckInProcesses')
BEGIN
    CREATE TABLE [dbo].[CheckInProcesses] (
        [Id]         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PropertyId] INT NOT NULL UNIQUE,
        [Title]      NVARCHAR(200) NOT NULL,
        [StepsJson]  NVARCHAR(MAX) NOT NULL,
        [CreatedAt]  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_CheckInProcesses_HostProperties] FOREIGN KEY ([PropertyId]) REFERENCES [dbo].[HostProperties] ([Id]) ON DELETE CASCADE
    );
END
GO

-- 16. GuestCheckIns Table (1:1 with Booking)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GuestCheckIns')
BEGIN
    CREATE TABLE [dbo].[GuestCheckIns] (
        [Id]               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BookingId]        INT NOT NULL UNIQUE,
        [CheckInProcessId] INT NOT NULL,
        [Status]           INT NOT NULL DEFAULT 0, -- 0: Pending, 1: InReview, 2: Approved, 3: Rejected
        [CreatedAt]        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [HostRemarks]      NVARCHAR(500) NULL,
        CONSTRAINT [FK_GuestCheckIns_Bookings] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GuestCheckIns_CheckInProcesses] FOREIGN KEY ([CheckInProcessId]) REFERENCES [dbo].[CheckInProcesses] ([Id])
    );
END
GO

-- 17. GuestDocuments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GuestDocuments')
BEGIN
    CREATE TABLE [dbo].[GuestDocuments] (
        [Id]             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [GuestCheckInId] INT NOT NULL,
        [DocumentType]   NVARCHAR(100) NOT NULL,
        [FileName]       NVARCHAR(200) NOT NULL,
        [Status]         INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Verified, 2: Rejected
        [UploadedAt]     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_GuestDocuments_GuestCheckIns] FOREIGN KEY ([GuestCheckInId]) REFERENCES [dbo].[GuestCheckIns] ([Id]) ON DELETE CASCADE
    );
END
GO

-- =========================================================================
-- SEED DATA (Roles & Standard Amenities)
-- =========================================================================
IF NOT EXISTS (SELECT * FROM [AspNetRoles] WHERE [Name] = 'SuperAdmin')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES 
    (NEWID(), 'SuperAdmin', 'SUPERADMIN', NEWID()),
    (NEWID(), 'Admin', 'ADMIN', NEWID()),
    (NEWID(), 'Host', 'HOST', NEWID()),
    (NEWID(), 'Guest', 'GUEST', NEWID());
END
GO

IF NOT EXISTS (SELECT * FROM [Amenities])
BEGIN
    INSERT INTO [Amenities] ([Name], [IconClass])
    VALUES
    ('Fast Wi-Fi', 'bi-wifi'),
    ('Swimming Pool', 'bi-water'),
    ('Free Parking', 'bi-car-front'),
    ('Air Conditioning', 'bi-snow'),
    ('Chef''s Kitchen', 'bi-cup-hot'),
    ('Dedicated Workspace', 'bi-laptop'),
    ('HD Smart TV', 'bi-tv'),
    ('Washer & Dryer', 'bi-shield-check'),
    ('Patio & Braai Area', 'bi-fire'),
    ('Sea View', 'bi-binoculars');
END
GO

PRINT 'Staybnb schema and initial seed data created successfully.';
