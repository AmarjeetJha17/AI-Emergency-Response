using System.Text.Json;

namespace EmergencyResponse.CopilotEvaluation.Infrastructure;

public sealed class EvaluationCaseLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<int> CountCasesAsync(
        string datasetDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(datasetDirectory))
        {
            throw new DirectoryNotFoundException($"Evaluation dataset directory not found: {datasetDirectory}");
        }

        var count = 0;
        foreach (var path in Directory.EnumerateFiles(datasetDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            await using var stream = File.OpenRead(path);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                count += root.GetArrayLength();
            }
            else if (root.ValueKind == JsonValueKind.Object &&
                     root.TryGetProperty("cases", out var cases) && cases.ValueKind == JsonValueKind.Array)
            {
                count += cases.GetArrayLength();
            }
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("id", out _))
            {
                count++;
            }
        }

        return count;
    }

    public async Task<IReadOnlyList<EvaluationCase<TInput, TExpected>>> LoadAsync<TInput, TExpected>(
        string datasetDirectory,
        string datasetVersion,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(datasetDirectory))
        {
            throw new DirectoryNotFoundException($"Evaluation dataset directory not found: {datasetDirectory}");
        }

        var cases = new List<EvaluationCase<TInput, TExpected>>();
        foreach (var path in Directory.EnumerateFiles(datasetDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            await using var stream = File.OpenRead(path);
            using var document = await JsonDocument.ParseAsync(
                stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var rows = root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray().ToArray()
                : root.ValueKind == JsonValueKind.Object &&
                  root.TryGetProperty("cases", out var caseArray) &&
                  caseArray.ValueKind == JsonValueKind.Array
                    ? caseArray.EnumerateArray().ToArray()
                    : [root];

            foreach (var row in rows)
            {
                if (!row.TryGetProperty("id", out var idProperty) ||
                    string.IsNullOrWhiteSpace(idProperty.GetString()))
                {
                    continue;
                }

                if (!row.TryGetProperty("input", out var input) ||
                    !row.TryGetProperty("expected", out var expected))
                {
                    continue;
                }

                var parsedInput = input.Deserialize<TInput>(JsonOptions);
                var parsedExpected = expected.Deserialize<TExpected>(JsonOptions);
                if (parsedInput is null || parsedExpected is null)
                {
                    throw new InvalidDataException($"Case '{idProperty.GetString()}' in '{path}' has invalid input or expected data.");
                }

                cases.Add(new EvaluationCase<TInput, TExpected>
                {
                    Id = idProperty.GetString()!,
                    Description = GetString(row, "description"),
                    Category = GetString(row, "category"),
                    DatasetVersion = datasetVersion,
                    Input = parsedInput,
                    Expected = parsedExpected,
                    CandidateOutput = row.TryGetProperty("candidateOutput", out var candidate)
                        ? candidate.Clone()
                        : null
                });
            }
        }

        var duplicate = cases.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Evaluation case ID '{duplicate.Key}' appears more than once in '{datasetDirectory}'.");
        }

        return cases;
    }

    public async Task<EvaluationDatasetManifest> LoadManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<EvaluationDatasetManifest>(
            stream, JsonOptions, cancellationToken);
        if (manifest is null || string.IsNullOrWhiteSpace(manifest.DatasetVersion) ||
            string.IsNullOrWhiteSpace(manifest.SchemaVersion))
        {
            throw new InvalidDataException($"Dataset manifest '{manifestPath}' is missing version metadata.");
        }

        return manifest;
    }

    private static string GetString(JsonElement row, string name)
        => row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
