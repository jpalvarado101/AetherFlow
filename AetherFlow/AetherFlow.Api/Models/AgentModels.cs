namespace AetherFlow.Api.Models;

public record AgentContext(
    string TaskType,
    string Instruction,
    string? InputContext,
    string? Domain,
    string? Plan,
    string? AggregatedNotes,
    string? IntermediateOutput
);

public record AgentResult(
    string AgentName,
    string Role,
    string Output,
    bool GovernancePassed,
    IReadOnlyList<string> GovernanceIssues
);
