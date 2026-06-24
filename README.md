# PermitDesk

Internal portal for managing **spridningstillstånd** (dissemination permits from Lantmäteriet) for drone material at Fastighetsbyrån Öland.

The core question PermitDesk answers for any uploaded drone material:

> **Får detta material publiceras just nu?** (JA / NEJ / OSÄKERT)

## What it does

- Tracks each drone job as a case from intake to approved/rejected decision
- Owns process state, status history, and publication eligibility
- Stores files in OneDrive/SharePoint (locally on disk in dev)
- Reads the shared Lantmäteriet mailbox, classifies mail, saves it as PDF, and updates status
- Surfaces unmatched mail for manual review rather than silently dropping it

The database is the source of truth for **status and metadata**. OneDrive/SharePoint is the source of truth for **files**.

## Stack

ASP.NET Core (Blazor Web App) · MudBlazor · PostgreSQL · Entity Framework Core · .NET 10

Later: Microsoft Graph (storage + mailbox), Playwright (Lantmäteriet form automation).

## Status

Pre-MVP. See open issues for current work.

## Run locally

Requires .NET 10 SDK and a running PostgreSQL.

1. Create the database and role (one-time):

   ```sh
   psql -U postgres -c "CREATE ROLE permitdesk WITH LOGIN PASSWORD 'permitdesk';"
   psql -U postgres -c "CREATE DATABASE permitdesk_dev OWNER permitdesk;"
   ```

2. Create `src/Voluble.PermitDesk.Web/appsettings.Local.json` (gitignored) with the connection string:

   ```json
   {
     "ConnectionStrings": {
       "PermitDesk": "Host=localhost;Port=5432;Database=permitdesk_dev;Username=permitdesk;Password=permitdesk"
     },
     "Storage": {
       "Local": {
         "Root": "C:\\path\\to\\local\\storage"
       }
     }
   }
   ```

   `Storage:Local:Root` is where case folders are created on disk. Pick any absolute path; it'll be created on first use.

3. Apply migrations and run:

   ```sh
   cd src/Voluble.PermitDesk.Web
   dotnet ef database update
   dotnet run
   ```

4. (Optional) Seed 15 realistic test cases:

   ```sh
   dotnet run --project src/Voluble.PermitDesk.Web -- seed
   ```

   Idempotent — re-running skips cases already on disk.

## Repo conventions

- Small, focused commits — one logical change per commit
- One issue per piece of work, tracked on the project board
- Internal planning lives outside the repo (Obsidian vault, ignored via `.gitignore`)

---

Built by [Voluble](https://voluble.io).
