using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;

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

        await SeedIncidentTypeResourceRequirementsAsync(context, cancellationToken);
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

    private static async Task SeedIncidentTypeResourceRequirementsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        if (await context.IncidentTypeResourceRequirements.AnyAsync(cancellationToken))
        {
            return;
        }

        var types = await context.IncidentTypes.ToDictionaryAsync(t => t.Code, cancellationToken);
        var requirements = new List<IncidentTypeResourceRequirement>();

        void AddReq(string typeCode, ResourceType resourceType, int min, int pref, bool isMandatory, int priority)
        {
            if (types.TryGetValue(typeCode, out var incidentType))
            {
                requirements.Add(new IncidentTypeResourceRequirement
                {
                    Id = Guid.NewGuid(),
                    IncidentTypeId = incidentType.Id,
                    ResourceType = resourceType,
                    MinimumCount = min,
                    PreferredCount = pref,
                    IsMandatory = isMandatory,
                    PriorityOrder = priority,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        // Flood
        AddReq("FLOOD", ResourceType.RescueTeam, 1, 2, true, 1);
        AddReq("FLOOD", ResourceType.Boat, 1, 2, true, 2);
        AddReq("FLOOD", ResourceType.Ambulance, 1, 1, false, 3);

        // Fire
        AddReq("FIRE", ResourceType.FireTruck, 1, 2, true, 1);
        AddReq("FIRE", ResourceType.Ambulance, 1, 1, true, 2);
        AddReq("FIRE", ResourceType.PoliceVehicle, 1, 1, false, 3);

        // Building Collapse
        AddReq("BUILDING_COLLAPSE", ResourceType.RescueTeam, 1, 2, true, 1);
        AddReq("BUILDING_COLLAPSE", ResourceType.Ambulance, 1, 2, true, 2);
        AddReq("BUILDING_COLLAPSE", ResourceType.Excavator, 1, 1, false, 3);

        // Road Accident
        AddReq("ROAD_ACCIDENT", ResourceType.PoliceVehicle, 1, 1, true, 1);
        AddReq("ROAD_ACCIDENT", ResourceType.Ambulance, 1, 1, true, 2);
        AddReq("ROAD_ACCIDENT", ResourceType.RescueVehicle, 1, 1, false, 3);

        // Medical Emergency
        AddReq("MEDICAL", ResourceType.Ambulance, 1, 2, true, 1);
        AddReq("MEDICAL", ResourceType.MedicalTeam, 1, 1, false, 2);

        // Chemical Leak
        AddReq("CHEMICAL_LEAK", ResourceType.FireTruck, 1, 1, true, 1);
        AddReq("CHEMICAL_LEAK", ResourceType.RescueTeam, 1, 1, true, 2);
        AddReq("CHEMICAL_LEAK", ResourceType.Ambulance, 1, 1, false, 3);

        // Landslide
        AddReq("LANDSLIDE", ResourceType.RescueTeam, 1, 2, true, 1);
        AddReq("LANDSLIDE", ResourceType.Excavator, 1, 1, true, 2);
        AddReq("LANDSLIDE", ResourceType.Ambulance, 1, 1, false, 3);

        // Earthquake
        AddReq("EARTHQUAKE", ResourceType.RescueTeam, 2, 4, true, 1);
        AddReq("EARTHQUAKE", ResourceType.MedicalTeam, 1, 2, true, 2);
        AddReq("EARTHQUAKE", ResourceType.Ambulance, 2, 3, true, 3);

        // Cyclone
        AddReq("CYCLONE", ResourceType.RescueTeam, 1, 2, true, 1);
        AddReq("CYCLONE", ResourceType.Boat, 1, 2, false, 2);
        AddReq("CYCLONE", ResourceType.Ambulance, 1, 1, false, 3);

        if (requirements.Count > 0)
        {
            await context.IncidentTypeResourceRequirements.AddRangeAsync(requirements, cancellationToken);
        }
    }
}