# Staybnb - Managed Property Rental Platform

A monolithic **ASP.NET Core 8.0 MVC** web application developed with **Entity Framework Core** and **Microsoft SQL Server** for the **PRO 742 (Programming 742)** undergraduate curriculum.

Inspired by the core functionality and design of Airbnb, **Staybnb** delivers a managed marketplace emphasizing the complete user lifecycle: exploring active listings as a **Guest**, applying to become a **Host**, and administering security and marketplace quality as an **Admin / SuperAdmin**.

---

## 1. Architectural Highlights (College & Rubric Constraints)
- **Monolithic MVC Architecture**: Direct dependency injection of `ApplicationDbContext`, `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, and `RoleManager<IdentityRole>` into controllers. Clean, undergraduate standard without unnecessary layers (no CQRS, MediatR, AutoMapper, or generic repository boilerplate).
- **Frontend Aesthetic**: Styled cleanly after Airbnb utilizing **Bootstrap 5**, **Bootstrap Icons**, Tag Helpers (`asp-for`, `asp-action`, `asp-route-id`), and minimal vanilla JavaScript for real-time price calculation and alert dismissals.
- **Pure Database-Backed Communication**: Messaging (`Message`) and notifications (`Notification`) are purely database-driven with SQL tables (no SignalR, WebSockets, or background workers).
- **Local File System Storage**: Property photos and guest identity verification documents are saved locally to `wwwroot/uploads/properties/` and `wwwroot/uploads/documents/` with relative database paths.

---

## 2. Default Seed Accounts (For Markers / Examiners)

| Persona | Email | Password | Assigned Role | Capabilities |
| :--- | :--- | :--- | :--- | :--- |
| **SuperAdmin** | `admin@staybnb.com` | `Admin@123` | `SuperAdmin`, `Admin` | Full system audit, approve/reject host applications, promote users to Admin role |
| **Host** | `host@staybnb.com` | `Host@123` | `Host` | Manage listings, toggle active status, approve/reject bookings, verify guest ID documents |
| **Guest** | `guest@staybnb.com` | `Guest@123` | `Guest` | Browse active stays, reserve trips, dynamic pricing breakdown, upload ID for check-in |

> *All new registrations via `/Account/Register` automatically receive the **Guest** role.*

---

## 3. Database Schema: 15 Core Entities

All 15 required entities are configured with exact keys, data types, and cascade deletion rules (`DeleteBehavior.Restrict` on cycles) in `Data/ApplicationDbContext.cs`:

1. **`ApplicationUser`** (`IdentityUser`): Extends Identity with `FirstName`, `LastName`, `ProfilePictureUrl`, and `CreatedAt`.
2. **`HostProperty`**: `Id`, `Title` (max 100), `Description`, `PricePerNight` (`decimal(18,2)`), `HostId` (FK), `Address`, `IsActive`.
3. **`Booking`**: `Id`, `PropertyId` (FK), `GuestId` (FK), `CheckInDate`, `CheckOutDate`, `TotalPrice` (`decimal(18,2)`), `Status` (`BookingStatus` enum).
4. **`Payment`**: `Id`, `BookingId` (FK, 1:1), `Amount` (`decimal(18,2)`), `Status` (`PaymentStatus` enum), `CreatedAt`.
5. **`Review`**: `Id`, `PropertyId` (FK), `ReviewerId` (FK), `Rating` (1 to 5), `Comment` (max 1000), `CreatedAt`.
6. **`Amenity`**: `Id`, `Name` (max 50), `IconClass` (max 50). Many-to-many relationship with `HostProperty`.
7. **`PropertyImage`**: `Id`, `PropertyId` (FK), `ImageUrl` (max 500).
8. **`WishlistItem`**: `Id`, `UserId` (FK), `PropertyId` (FK), `CreatedAt`.
9. **`Message`**: `Id`, `SenderId` (FK), `ReceiverId` (FK), `Content`, `Timestamp`.
10. **`ActivityLog`**: `Id`, `UserId` (FK), `Action` (max 100), `ActivityType` (max 100), `CreatedAt`.
11. **`Notification`**: `Id`, `UserId` (FK), `Title` (max 100), `Type`, `CreatedAt`, `IsRead`.
12. **`HostApplication`**: `Id`, `ApplicationUserId` (FK), `PropertyId` (FK), `Status` (`ApplicationStatus` enum), `AppliedAt`, `AdminNotes`.
13. **`CheckInProcess`**: `Id`, `PropertyId` (FK, 1:1), `Title` (max 200), `StepsJson`, `CreatedAt`.
14. **`GuestCheckIn`**: `Id`, `BookingId` (FK, 1:1), `CheckInProcessId` (FK), `Status` (`CheckInStatus` enum), `CreatedAt`, `HostRemarks`.
15. **`GuestDocument`**: `Id`, `GuestCheckInId` (FK), `DocumentType` (max 100), `FileName` (max 200), `Status` (`DocumentStatus` enum), `UploadedAt`.

---

## 4. Critical Business Logic & Rubric Scoring

### 1. Identity & Mutual Exclusivity Transition
- Newly registered users receive the **Guest** role.
- When an Administrator approves a `HostApplication` in `/Admin/ApplicationDetails`:
  1. System adds the **Host** role.
  2. System removes the **Guest** role (mutual exclusivity enforced: strictly Guest OR Host, never both).
  3. `HostProperty.IsActive` is set to `true`.
  4. `HostApplication.Status` is set to `Approved`.
  5. Audit entry logged to `ActivityLog` and notification created in `Notification`.

### 2. Mandatory Host Application Photo Submission
- In `/Host/BecomeHost`, guests must provide listing details and at least one property photo (`IFormFile`).
- Server-side validation strictly enforces this and rejects submissions without images.

### 3. Booking Calculation Engine
- Exact rubric formula implemented in both controller and Razor view:
  $$\text{TotalPrice} = (\text{PricePerNight} \times \text{Nights}) + \text{CleaningFee} + \text{ServiceFee}$$
  - Default `CleaningFee` = **R 350.00**
  - Default `ServiceFee` = **12% of Room Total**

### 4. Multi-Step Check-in & ID Verification Workflow
- When a booking is `Approved`, the Guest unlocks the check-in portal (`/Booking/CheckIn`).
- Guest reviews property arrival rules and uploads a verification document (National ID or Passport).
- Uploaded file is saved to `wwwroot/uploads/documents/`, `GuestDocument` is created, and status transitions to `InReview`.
- The Host reviews the uploaded file via `/Host/VerifyCheckIn` and approves or rejects the document with remarks.

### 5. Audit Logging & Notification Center
- Every user login, registration, host application submission, admin approval/rejection, booking creation/status transition, and check-in event is recorded in the `ActivityLog` table.
- Notifications are created for booking confirmations, application reviews, and document verification updates, viewable with unread count badges in `/Communication/Notifications`.

---

## 5. How to Run the Application

### Option A: Using .NET CLI
```bash
cd Staybnb
dotnet run
```
Navigate to: `https://localhost:5001` or `http://localhost:5000`

### Option B: Using Visual Studio 2022
1. Open the folder or `.csproj` in Visual Studio.
2. Ensure connection string in `appsettings.json` points to your local SQL Server / LocalDB instance:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StaybnbDb;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```
3. Press **F5** or click **Run**. Database creation and seed data will execute automatically on startup.

### Option C: Manual SQL Server Script
If you prefer running manual SQL scripts in SQL Server Management Studio (SSMS), execute the provided script:
- `Staybnb_Schema_And_Data.sql`
