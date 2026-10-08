using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Safety;

namespace EmergencyResponse.CopilotEvaluation.Infrastructure;

public sealed class EvaluationModelFactory
{
    public string CandidateDeployment => Required("AI_EVAL_CANDIDATE_DEPLOYMENT");
    public string JudgeDeployment => Required("AI_EVAL_JUDGE_DEPLOYMENT");

    public IChatClient CreateCandidateClient()
        => CreateClient("AI_EVAL_CANDIDATE_ENDPOINT", CandidateDeployment);

    public IChatClient CreateJudgeClient()
        => CreateClient("AI_EVAL_JUDGE_ENDPOINT", JudgeDeployment);

    public ReportingConfiguration CreateQualityReporting(
        string cachePath,
        string runId)
    {
        var judge = CreateJudgeClient();
        IEvaluator[] evaluators =
        [
            new RelevanceEvaluator(),
            new CompletenessEvaluator(),
            new GroundednessEvaluator(),
            new TaskAdherenceEvaluator()
        ];

        return DiskBasedReportingConfiguration.Create(
            storageRootPath: cachePath,
            evaluators: evaluators,
            chatConfiguration: new ChatConfiguration(judge),
            enableResponseCaching: UseResponseCache(),
            executionName: runId);
    }

    public ReportingConfiguration CreateSafetyReporting(
        string cachePath,
        string runId)
    {
        var required = new[]
        {
            "AI_EVAL_SUBSCRIPTION_ID",
            "AI_EVAL_RESOURCE_GROUP",
            "AI_EVAL_FOUNDRY_PROJECT"
        };
        foreach (var name in required)
        {
            _ = Required(name);
        }

        var safetyService = new ContentSafetyServiceConfiguration(
            credential: new DefaultAzureCredential(),
            subscriptionId: Required("AI_EVAL_SUBSCRIPTION_ID"),
            resourceGroupName: Required("AI_EVAL_RESOURCE_GROUP"),
            projectName: Required("AI_EVAL_FOUNDRY_PROJECT"));

        IEvaluator[] evaluators =
        [
            new IndirectAttackEvaluator(),
            new ProtectedMaterialEvaluator()
        ];

        return DiskBasedReportingConfiguration.Create(
            storageRootPath: cachePath,
            evaluators: evaluators,
            chatConfiguration: safetyService.ToChatConfiguration(CreateCandidateClient()),
            enableResponseCaching: UseResponseCache(),
            executionName: runId);
    }

    private static IChatClient CreateClient(string endpointSetting, string deployment)
    {
        var endpoint = new Uri(Required(endpointSetting));
        var client = new AzureOpenAIClient(endpoint, new DefaultAzureCredential());
        return client.GetChatClient(deployment).AsIChatClient();
    }

    private static string Required(string name)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Live evaluation requires environment variable '{name}'.");

    private static bool UseResponseCache()
        => !bool.TryParse(Environment.GetEnvironmentVariable("AI_EVAL_DISABLE_CACHE"), out var disabled) || !disabled;
}
