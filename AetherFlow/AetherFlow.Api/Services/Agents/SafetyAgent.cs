using System.Text.Json;
using AetherFlow.Api.Models;
using AetherFlow.Api.Services;

namespace AetherFlow.Api.Services.Agents;

public class SafetyAgent : IAgent
{
    public string Name => "SafetyAgent";
    public string Role => "Safety";

    private readonly IOpenAIService _openAI;

    public SafetyAgent(IOpenAIService openAI)
    {
        _openAI = openAI;
    }

    public async Task<AgentResult> RunAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var systemPrompt =
            "You are a safety/governance agent. Review the draft answer for harmful, unsafe, or disallowed content. " +
            "Return STRICT JSON: {"safe": true/false, "issues": ["..."]}. " +
            "If uncertain, set safe=false.";

        var userPrompt = $@"
Task type: {context.TaskType}
Instruction: {context.Instruction}

Draft answer:
{context.IntermediateOutput ?? ""}

Return ONLY the JSON (no markdown, no prose).";

        var json = await _openAI.ChatAsync(systemPrompt, userPrompt, cancellationToken);

        var issues = new List<string>();
        var safe = true;

        try
        {
            var parsed = JsonSerializer.Deserialize<SafetyResult>(json);
            if (parsed is not null)
            {
                safe = parsed.Safe;
                if (parsed.Issues is not null)
                    issues.AddRange(parsed.Issues.Where(i => !string.IsNullOrWhiteSpace(i)));
            }
            else
            {
                safe = false;
                issues.Add("SafetyAgent: failed to parse JSON, treating as unsafe.");
            }
        }
        catch
        {
            safe = false;
            issues.Add("SafetyAgent: JSON parsing exception, treating as unsafe.");
        }

        return new AgentResult(
            AgentName: Name,
            Role: Role,
            Output: json.Trim(),
            GovernancePassed: safe,
            GovernanceIssues: issues
        );
    }

    private sealed class SafetyResult
    {
        public bool Safe { get; set; }
        public List<string>? Issues { get; set; }
    }
}
