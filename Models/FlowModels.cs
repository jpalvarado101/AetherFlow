namespace AetherFlow.Api.Models;

public record FlowRunRequest(
    string TaskType,
    string Instruction,
    string? InputContext,
    string? Domain
);

public record FlowRunResponse(
    Guid RunId,
    string TaskType,
    string Instruction,
    string? InputContext,
    string? Domain,
    string Plan,
    IReadOnlyList<AgentExecutionRecord> Steps,
    string FinalOutput
);

public record AgentExecutionRecord(
    string AgentName,
    string Role,
    string InputSummary,
    string OutputSummary,
    bool GovernancePassed,
    IReadOnlyList<string> GovernanceIssues
);

public record ExperimentSummary(
    Guid RunId,
    DateTimeOffset Timestamp,
    string TaskType,
    string AgentPath,
    bool GovernancePassed,
    int StepCount
);
