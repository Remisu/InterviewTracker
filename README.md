# InterviewTracker

> A personal job application management system built with **.NET 10 and Blazor**, designed to organize and track the entire recruitment process from the initial application to the final offer.

## About the Project

**InterviewTracker** was created to solve a problem I encountered during my own job search: keeping track of multiple applications, recruiters, interviews, follow-ups, job requirements and application statuses across different platforms.

Instead of managing everything through spreadsheets and notes, InterviewTracker provides a centralized workspace where the entire recruitment pipeline can be tracked.

The project is also an opportunity to explore modern **.NET development with Blazor, Entity Framework Core and SQLite** while building a tool based on a real-world use case.

## Key Features

### Job Application Tracking

Manage job opportunities with information such as:

- Job title
- Client company
- Recruiting company
- Job description and reference
- Job posting URL
- Location and country
- Remote / Hybrid / On-site work mode
- Employment type
- Salary range and currency
- Interest level
- Application status

### Application Pipeline

Track applications throughout the recruitment lifecycle:

`Applied / Waiting → Recruiter Screening → Client Interview → Technical Assessment → Final Interview`

Applications can also be marked as:

`Rejected` · `No Response`

Active cards show local calendar days since meaningful activity, longest waiting first. After 12 days they receive an awaiting-reply highlight. This does not change their stage. Rejected and manually marked No Response cards have compact terminal columns and can be restored to an active stage.

Legacy Applied and Waiting values share the first column. Interested, Interview, Offer, Accepted, and Withdrawn retain their original values in a **Legacy stages · review** area until you deliberately choose a current stage. No migration guesses an ambiguous stage.

### Application Timeline

Each application maintains an activity history.

Interactions with recruiters or companies can be recorded as timeline entries, making it easy to understand what happened during the recruitment process and when.

Examples:

- Application submitted
- Recruiter contacted
- Recruiter response
- Interview scheduled
- Follow-up sent
- Technical interview completed
- Offer received

### Interview Management

Track the different interview stages associated with an application, keeping the recruitment process organized even when multiple interview rounds are involved.

### Contacts

Store contacts related to each application, such as recruiters, hiring managers and other people involved in the recruitment process.

### Technology Tracking

Job requirements can be associated with technologies, allowing technical requirements to be structured instead of being stored only inside the job description.

### International Job Search Support

The application also tracks information particularly useful when applying internationally:

- Visa sponsorship availability
- Relocation requirements
- Required Japanese level
- Required English level
- Country and city
- Salary and currency

### Follow-up Management

Applications store:

- Application date
- Last activity date
- Next follow-up date
- Days since last activity

This makes it easier to identify applications that require attention.

---

## Tech Stack

| Technology | Usage |
|---|---|
| **C#** | Main programming language |
| **.NET 10** | Application platform |
| **Blazor** | Web UI |
| **ASP.NET Core** | Web application backend |
| **Entity Framework Core** | ORM and data access |
| **SQLite** | Local relational database |
| **EF Core Migrations** | Database schema management |

---

## Architecture

The solution separates responsibilities across different areas of the application:

```text
InterviewTracker/
│
├── Components/      # Blazor UI components
├── Data/            # Database context and persistence
├── Helpers/         # Shared helper logic
├── Migrations/      # Entity Framework Core migrations
├── Models/          # Domain entities and enums
├── Services/        # Application/business logic
└── wwwroot/         # Static assets
```

The domain model includes entities such as:

```text
JobApplication
 ├── Contacts
 ├── Interviews
 ├── Activities
 └── TechnologyRequirements
```

This keeps application data structured while allowing each job application to maintain its own recruitment history.

---

## Getting Started

### Requirements

- .NET 10 SDK
- Visual Studio 2022+ / JetBrains Rider / VS Code

### Clone the repository

```bash
git clone https://github.com/Remisu/InterviewTracker.git
cd InterviewTracker
```

### Restore dependencies

```bash
dotnet restore
```

### Apply database migrations

```bash
dotnet ef database update --project InterviewTracker
```

### Run the application

```bash
dotnet run --project InterviewTracker
```

## File synchronization and database compatibility

Use **Import / Export** to download a version 1 JSON export, transfer it to another computer, and preview its import. Confirm the merge after choosing a local or imported version for every equal-timestamp conflict. Newer imported records update the complete application aggregate; older versions are skipped and destination-only applications stay intact. Whole-application deletions do not propagate. Conflicts are resolved with a new UTC modification timestamp without changing the activity clock. A changed local record invalidates the preview so you can review it again.

Exports contain stable application GUIDs, UTC creation/modification timestamps, contacts, interviews and questions, activity history, and required technologies. Numeric IDs inside each application are portable references and are remapped on import. Shared technologies are matched by exact name; existing local catalog metadata and the local skill profile are preserved. Secrets and machine settings are excluded. Limits: 20 MB and 10,000 applications per file.

Migration `20261021134807_SyncIdentity` runs automatically at startup. It adds a unique `SyncId` to every application, creates an installation identifier, and initializes missing activity timestamps from existing update dates. It preserves application IDs, relationships, stage integers, priority values, and existing creation/modification dates. Language fields already existed, so their data and custom text values remain compatible with the new selectors. Back up your database before upgrading either computer.

For the first transfer, upgrade the source computer and export from it, then import into a fresh destination installation. Two independently upgraded copies of an old database receive different GUIDs for their pre-sync records: they cannot be deduplicated safely by company, title, or numeric ID. Reconcile pre-existing independent copies before starting synchronization; future exports preserve the GUIDs. The installation identifier belongs to the database, so copying the complete database also copies that identifier.

The SQLite path defaults to `InterviewTracker/interviewtracker.db`. Tests or isolated runs can override it with `--DatabasePath /absolute/path/test.db`.

## Validation

```bash
dotnet build InterviewTracker.slnx
dotnet test InterviewTracker.slnx
```

The 33 regression and component tests use temporary SQLite databases and bUnit. They exercise drag-and-drop into terminal stages and restoration, form defaults and manual dates, save failures and retries, post-navigation notifications, the import preview/confirmation UI, migrations, company validation, language persistence, meaningful activity, calendar-day ordering, status transitions, idempotent merges, UTC version comparisons, conflict resolution, stale previews, relationship remapping, and transactional rollback.

---

## Motivation

This project started as a tool for managing my own job search.

When applying to multiple positions simultaneously, information quickly becomes fragmented across LinkedIn, recruiting agencies, emails and company career pages.

I wanted a system that could answer questions such as:

- Which applications am I still waiting for?
- When was the last time a recruiter contacted me?
- Which applications need a follow-up?
- What technologies does each position require?
- Which companies offer visa sponsorship?
- How many interview rounds have I completed?
- Which opportunities are currently the highest priority?

Building InterviewTracker allowed me to turn that real-world problem into a practical software project.

---

## Roadmap

Planned improvements include:

- [ ] Advanced filtering and search
- [ ] Application analytics
- [ ] Interview calendar
- [ ] Follow-up reminders
- [ ] Improved Kanban interactions
- [ ] CV / job description matching
- [ ] Technology match scoring
- [ ] Import job information from URLs
- [x] File-based application import / export
- [ ] Authentication and multiple profiles
- [ ] Deployment and online demo

---

## What This Project Demonstrates

InterviewTracker demonstrates practical experience with:

- C# and modern .NET
- Blazor application development
- Relational data modeling
- Entity Framework Core
- Database migrations
- Component-based UI development
- Application state and workflow modeling
- Service-oriented code organization
- Building software around real-world requirements

---

## Author

**Remisu** 

Software Developer focused on **C# / .NET and web development**.

This project is part of my personal software development portfolio.

## License

This project is intended primarily as a personal and portfolio project.
