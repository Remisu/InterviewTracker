using InterviewTracker.Models;
using InterviewTracker.Models.Enums;

namespace InterviewTracker.Data;

public static class SeedData
{
    public static async Task SeedTechnologiesAsync(AppDbContext db)
    {
        if (db.Technologies.Any())
            return;

        var technologies = new List<Technology>
        {
            // Programming Languages
            new() { Name = "C#",                Category = TechnologyCategory.ProgrammingLanguage },
            new() { Name = "JavaScript",        Category = TechnologyCategory.ProgrammingLanguage },
            new() { Name = "TypeScript",        Category = TechnologyCategory.ProgrammingLanguage },
            new() { Name = "Python",            Category = TechnologyCategory.ProgrammingLanguage },
            new() { Name = "Java",              Category = TechnologyCategory.ProgrammingLanguage },
            new() { Name = "Go",                Category = TechnologyCategory.ProgrammingLanguage },

            // Frameworks / Backend
            new() { Name = ".NET",              Category = TechnologyCategory.Framework },
            new() { Name = "ASP.NET Core",      Category = TechnologyCategory.Framework },
            new() { Name = "Blazor",            Category = TechnologyCategory.Framework },
            new() { Name = "WPF",               Category = TechnologyCategory.Framework },
            new() { Name = "Entity Framework",  Category = TechnologyCategory.Framework },
            new() { Name = "Dapper",            Category = TechnologyCategory.Framework },
            new() { Name = "Node.js",           Category = TechnologyCategory.Framework },
            new() { Name = "React",             Category = TechnologyCategory.Frontend },
            new() { Name = "Angular",           Category = TechnologyCategory.Frontend },
            new() { Name = "Vue.js",            Category = TechnologyCategory.Frontend },
            new() { Name = "HTML",              Category = TechnologyCategory.Frontend },
            new() { Name = "CSS",               Category = TechnologyCategory.Frontend },

            // Architecture
            new() { Name = "REST API",          Category = TechnologyCategory.Architecture },
            new() { Name = "Microservices",     Category = TechnologyCategory.Architecture },
            new() { Name = "CQRS",              Category = TechnologyCategory.Architecture },
            new() { Name = "DDD",               Category = TechnologyCategory.Architecture },
            new() { Name = "SOLID",             Category = TechnologyCategory.Architecture },
            new() { Name = "gRPC",              Category = TechnologyCategory.Architecture },
            new() { Name = "SignalR",           Category = TechnologyCategory.Architecture },

            // Databases
            new() { Name = "SQL Server",        Category = TechnologyCategory.Database },
            new() { Name = "PostgreSQL",        Category = TechnologyCategory.Database },
            new() { Name = "MySQL",             Category = TechnologyCategory.Database },
            new() { Name = "SQLite",            Category = TechnologyCategory.Database },
            new() { Name = "MongoDB",           Category = TechnologyCategory.Database },
            new() { Name = "Redis",             Category = TechnologyCategory.Database },
            new() { Name = "Elasticsearch",     Category = TechnologyCategory.Database },

            // Cloud
            new() { Name = "Azure",             Category = TechnologyCategory.Cloud },
            new() { Name = "AWS",               Category = TechnologyCategory.Cloud },
            new() { Name = "Google Cloud",      Category = TechnologyCategory.Cloud },

            // DevOps / Tools
            new() { Name = "Docker",            Category = TechnologyCategory.DevOps },
            new() { Name = "Kubernetes",        Category = TechnologyCategory.DevOps },
            new() { Name = "GitHub Actions",    Category = TechnologyCategory.DevOps },
            new() { Name = "Azure DevOps",      Category = TechnologyCategory.DevOps },
            new() { Name = "Terraform",         Category = TechnologyCategory.DevOps },
            new() { Name = "Git",               Category = TechnologyCategory.Tool },
            new() { Name = "RabbitMQ",          Category = TechnologyCategory.Tool },
            new() { Name = "Kafka",             Category = TechnologyCategory.Tool },

            // Testing
            new() { Name = "xUnit",             Category = TechnologyCategory.Testing },
            new() { Name = "NUnit",             Category = TechnologyCategory.Testing },
            new() { Name = "Moq",               Category = TechnologyCategory.Testing },
        };

        db.Technologies.AddRange(technologies);
        await db.SaveChangesAsync();
    }
}
