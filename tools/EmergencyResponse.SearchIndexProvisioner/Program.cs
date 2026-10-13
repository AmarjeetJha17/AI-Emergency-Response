using Azure.Identity;
using Azure.Search.Documents.Indexes;
using EmergencyResponse.SearchIndexProvisioner;

if (args.Length != 3 || !int.TryParse(args[2], out var dimensions) || dimensions <= 0)
{
    Console.Error.WriteLine(
        "Usage: EmergencyResponse.SearchIndexProvisioner <search-endpoint> <index-name> <embedding-dimensions>");
    return 2;
}

var client = new SearchIndexClient(
    new Uri(args[0]),
    new DefaultAzureCredential());
var index = KnowledgeIndexDefinition.Create(args[1], dimensions);
await client.CreateOrUpdateIndexAsync(index, cancellationToken: CancellationToken.None);
Console.WriteLine($"Search index '{args[1]}' is ready.");
return 0;
