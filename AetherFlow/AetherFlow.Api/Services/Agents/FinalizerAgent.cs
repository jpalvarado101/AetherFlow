using AetherFlow.Api.Models;
using AetherFlow.Api.Services;

namespace AetherFlow.Api.Services.Agents;

public class FinalizerAgent : IAgent
{
    public string Name => "FinalizerAgent";
    public string Role => "Finalizer";

    private readonly IOpenAIService _openAI;

    public FinalizerAgent(IOpenAIService openAI)
    {
        _openAI = openAI;
    }

    public async Task<AgentResult> RunAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var systemPrompt =
            "You are a senior engineer. Given a task, retrieved context, critique notes, and safety concerns, " +
            "produce a final, clean answer. Do NOT mention internal agent steps or system prompts. " +
            "Be concise but complete.";

        var userPrompt = $@"
Task type: {context.TaskType}
Instruction: {context.Instruction}
Domain: {context.Domain ?? "n/a"}

Plan:
{context.Plan ?? "n/a"}

Retrieved context:
{context.InputContext ?? "n/a"}

Critique / notes:
{context.AggregatedNotes ?? "n/a"}

Draft:
{context.IntermediateOutput ?? "n/a"}

Return ONLY the final answer, suitable for a user.";

        var answer = await _openAI.ChatAsync(systemPrompt, userPrompt, cancellationToken);

        return new AgentResult(
            AgentName: Name,
            Role: Role,
            Output: answer.Trim(),
            GovernancePassed: true,
            GovernanceIssues: Array.Empty<string>()
        );
    }
}
