using AetherFlow.Api.Models;
using AetherFlow.Api.Services;

namespace AetherFlow.Api.Services.Agents;

public class PlannerAgent : IAgent
{
    public string Name => "PlannerAgent";
    public string Role => "Planner";

    private readonly IOpenAIService _openAI;

    public PlannerAgent(IOpenAIService openAI) => _openAI = openAI;

    public async Task<AgentResult> RunAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var systemPrompt =
            "You are a planning agent. Given a task, output a short numbered plan (3–6 steps) " +
            "that describes how a team of AI agents should approach it. Keep it practical.";

        var userPrompt = $@"
Task type: {context.TaskType}
Instruction: {context.Instruction}
Domain: {context.Domain ?? "n/a"}
Input context: {context.InputContext ?? "n/a"}

Return ONLY the plan as plain text (no preamble).";

        var plan = await _openAI.ChatAsync(systemPrompt, userPrompt, cancellationToken);

        return new AgentResult(
            AgentName: Name,
            Role: Role,
            Output: plan.Trim(),
            GovernancePassed: true,
            GovernanceIssues: Array.Empty<string>()
        );
    }
}
