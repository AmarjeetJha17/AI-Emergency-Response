using System.Globalization;
using System.Text;
using EmergencyResponse.PredictiveTraining.Training;

namespace EmergencyResponse.PredictiveTraining.Data;

public static class EscalationTrainingDatasetGenerator
{
    public static List<EscalationModelInput> GenerateDataset(int count = 2500, int seed = 42)
    {
        var random = new Random(seed);
        var dataset = new List<EscalationModelInput>(count);

        for (int i = 0; i < count; i++)
        {
            // Determine if this snapshot represents a scenario that escalated within the next 6 hours
            // Realistic emergency dataset base rate: ~22% escalation rate
            bool shouldEscalate = random.NextDouble() < 0.22;

            float severityScore;
            float priorityScore;
            float peopleAffected;
            float vehiclesAffected;
            float casualties;
            float immediateDanger;
            float peopleTrapped;
            float criticalInfrastructure;
            float infrastructureDamage;
            float roadBlocked;
            float escalationCount;
            float duplicateCount;
            float relatedIncidentCount;
            float resourceShortage;
            float incidentAgeMinutes;

            if (shouldEscalate)
            {
                // Scenarios leading to escalation within 6 hours
                severityScore = random.Next(45, 95);
                priorityScore = random.Next(50, 95);
                peopleAffected = random.Next(5, 120);
                vehiclesAffected = random.Next(0, 25);
                casualties = random.NextDouble() > 0.4 ? random.Next(1, 15) : 0;
                immediateDanger = random.NextDouble() > 0.3 ? 1f : 0f;
                peopleTrapped = random.NextDouble() > 0.4 ? 1f : 0f;
                criticalInfrastructure = random.NextDouble() > 0.5 ? 1f : 0f;
                infrastructureDamage = random.NextDouble() > 0.4 ? 1f : 0f;
                roadBlocked = random.NextDouble() > 0.4 ? 1f : 0f;
                escalationCount = random.Next(0, 3);
                duplicateCount = random.Next(1, 10);
                relatedIncidentCount = random.Next(1, 8);
                resourceShortage = random.NextDouble() > 0.5 ? 1f : 0f;
                incidentAgeMinutes = random.Next(10, 360);
            }
            else
            {
                // Stable or minor scenarios that did not escalate in next 6 hours
                severityScore = random.Next(10, 55);
                priorityScore = random.Next(10, 50);
                peopleAffected = random.Next(0, 15);
                vehiclesAffected = random.Next(0, 4);
                casualties = random.NextDouble() > 0.95 ? 1f : 0f;
                immediateDanger = random.NextDouble() > 0.85 ? 1f : 0f;
                peopleTrapped = random.NextDouble() > 0.9 ? 1f : 0f;
                criticalInfrastructure = random.NextDouble() > 0.9 ? 1f : 0f;
                infrastructureDamage = random.NextDouble() > 0.85 ? 1f : 0f;
                roadBlocked = random.NextDouble() > 0.75 ? 1f : 0f;
                escalationCount = 0;
                duplicateCount = random.Next(0, 3);
                relatedIncidentCount = random.Next(0, 2);
                resourceShortage = random.NextDouble() > 0.85 ? 1f : 0f;
                incidentAgeMinutes = random.Next(5, 720);
            }

            dataset.Add(new EscalationModelInput
            {
                SeverityScore = severityScore,
                PriorityScore = priorityScore,
                PeopleAffected = peopleAffected,
                VehiclesAffected = vehiclesAffected,
                Casualties = casualties,
                ImmediateDanger = immediateDanger,
                PeopleTrapped = peopleTrapped,
                CriticalInfrastructureAffected = criticalInfrastructure,
                InfrastructureDamage = infrastructureDamage,
                RoadBlocked = roadBlocked,
                EscalationCount = escalationCount,
                DuplicateCount = duplicateCount,
                RelatedIncidentCount = relatedIncidentCount,
                ResourceShortage = resourceShortage,
                IncidentAgeMinutes = incidentAgeMinutes,
                Label = shouldEscalate
            });
        }

        return dataset;
    }

    public static void SaveToCsv(IEnumerable<EscalationModelInput> data, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SeverityScore,PriorityScore,PeopleAffected,VehiclesAffected,Casualties," +
                      "ImmediateDanger,PeopleTrapped,CriticalInfrastructureAffected,InfrastructureDamage," +
                      "RoadBlocked,EscalationCount,DuplicateCount,RelatedIncidentCount,ResourceShortage," +
                      "IncidentAgeMinutes,Label");

        foreach (var row in data)
        {
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{row.SeverityScore:F1},{row.PriorityScore:F1},{row.PeopleAffected:F0},{row.VehiclesAffected:F0},{row.Casualties:F0}," +
                $"{row.ImmediateDanger:F0},{row.PeopleTrapped:F0},{row.CriticalInfrastructureAffected:F0},{row.InfrastructureDamage:F0}," +
                $"{row.RoadBlocked:F0},{row.EscalationCount:F0},{row.DuplicateCount:F0},{row.RelatedIncidentCount:F0},{row.ResourceShortage:F0}," +
                $"{row.IncidentAgeMinutes:F1},{row.Label}"));
        }

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(filePath, sb.ToString());
    }
}
