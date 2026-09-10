using EmergencyResponse.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        await SeedIncidentTypesAsync(context, cancellationToken);
        await SeedDepartmentsAsync(context, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedIncidentTypesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        if (await context.IncidentTypes.AnyAsync(cancellationToken))
        {
            return;
        }

        var incidentTypes = new[]
        {
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Flood",
                Code = "FLOOD",
                Description = "Flooding affecting people, vehicles, roads or property.",
                IsActive = true
            },
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Fire",
                Code = "FIRE",
                Description = "Fire involving buildings, vehicles, vegetation or infrastructure.",
                IsActive = true
            },
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Road Accident",
                Code = "ROAD_ACCIDENT",
                Description = "Vehicle collision or other road traffic emergency.",
                IsActive = true
            },
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Landslide",
                Code = "LANDSLIDE",
                Description = "Landslide or mudslide affecting people, roads or infrastructure.",
                IsActive = true
            },
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Earthquake",
                Code = "EARTHQUAKE",
                Description = "Earthquake-related emergency or damage.",
                IsActive = true
            },
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Cyclone",
                Code = "CYCLONE",
                Description = "Cyclone-related emergency.",
                IsActive = true
            },
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Building Collapse",
                Code = "BUILDING_COLLAPSE",
                Description = "Structural collapse involving a building or similar structure.",
                IsActive = true
            },
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Medical Emergency",
                Code = "MEDICAL",
                Description = "Medical emergency requiring urgent response.",
                IsActive = true
            },
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Chemical Leak",
                Code = "CHEMICAL_LEAK",
                Description = "Chemical spill, leak or hazardous-material incident.",
                IsActive = true
            },
            new IncidentType
            {
                Id = Guid.NewGuid(),
                Name = "Other",
                Code = "OTHER",
                Description = "Emergency that does not fit another incident category.",
                IsActive = true
            }
        };

        await context.IncidentTypes.AddRangeAsync(
            incidentTypes,
            cancellationToken);
    }

    private static async Task SeedDepartmentsAsync(
    ApplicationDbContext context,
    CancellationToken cancellationToken)
    {
        if (await context.Departments.AnyAsync(cancellationToken))
        {
            return;
        }

        var departments = new[]
        {
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Police Department",
                Code = "POLICE",
                Description = "Police and public safety response.",
                IsActive = true
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Fire Department",
                Code = "FIRE",
                Description = "Fire suppression and rescue response.",
                IsActive = true
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Medical Services",
                Code = "MEDICAL",
                Description = "Emergency medical response.",
                IsActive = true
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Disaster Response",
                Code = "DISASTER",
                Description = "Disaster management and rescue operations.",
                IsActive = true
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Public Works",
                Code = "PWD",
                Description = "Road, infrastructure and public works response.",
                IsActive = true
            },
            new Department
            {
                Id = Guid.NewGuid(),
                Name = "Municipal Services",
                Code = "MUNICIPAL",
                Description = "Municipal emergency and civic services.",
                IsActive = true
            }
        };

        await context.Departments.AddRangeAsync(
            departments,
            cancellationToken);
    }
}