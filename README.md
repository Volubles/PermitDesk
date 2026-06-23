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

Pre-MVP. See open issues and the project board for current work.

## Repo conventions

- Small, focused commits — one logical change per commit
- One issue per piece of work, tracked on the project board
- Internal planning lives outside the repo (Obsidian vault, ignored via `.gitignore`)

---

Built by [Voluble](https://voluble.io).
