using AetherFlow.Api.Models;
using AetherFlow.Api.Services;

namespace AetherFlow.Api.Services.Agents;

public class CriticAgent : IAgent
{
    public string Name => "CriticAgent";
    public string Role => "Critic";

    private readonly IOpenAIService _openAI;

    public CriticAgent(IOpenAIService openAI)
    {
        _openAI = openAI;
    }

    public async Task<AgentResult> RunAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var systemPrompt =
            "You are a critical reviewer. Given a draft answer, list 3–5 concrete issues or improvements. " +
            "Be concise and practical.";

        var userPrompt = $@"
Task type: {context.TaskType}
Instruction: {context.Instruction}

Draft answer:
{context.IntermediateOutput ?? ""}

Return a bullet list of issues / improvements (no extra commentary).";

        var critique = await _openAI.ChatAsync(systemPrompt, userPrompt, cancellationToken);

        return new AgentResult(
            AgentName: Name,
            Role: Role,
            Output: critique.Trim(),
            GovernancePassed: true,
            GovernanceIssues: Array.Empty<string>()
        );
    }
}
