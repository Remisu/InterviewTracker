# InterviewTracker

> A personal job application management system built with **.NET 10 and Blazor**, designed to organize and track the entire recruitment process from the initial application to the final offer.

## About the Project

**InterviewTracker** was created to solve a problem I encountered during my own job search: keeping track of multiple applications, recruiters, interviews, follow-ups, job requirements and application statuses across different platforms.

Instead of managing everything through spreadsheets and notes, InterviewTracker provides a centralized dashboard where the entire recruitment pipeline can be tracked.

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
- Priority
- Application status

### Application Pipeline

Track applications throughout the recruitment lifecycle:

`Interested → Applied → Interview → Offer → Accepted`

Applications can also be marked as:

`Rejected` · `Withdrawn` · `No Response`

The system automatically identifies applications with **no activity for 12 days**, helping highlight opportunities that may require follow-up.

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

- [ ] Dashboard with job search statistics
- [ ] Advanced filtering and search
- [ ] Application analytics
- [ ] Interview calendar
- [ ] Follow-up reminders
- [ ] Improved Kanban interactions
- [ ] CV / job description matching
- [ ] Technology match scoring
- [ ] Import job information from URLs
- [ ] Export application history
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
