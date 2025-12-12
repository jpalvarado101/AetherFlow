using AetherFlow.Api.Models;
using AetherFlow.Api.Services.Agents;

namespace AetherFlow.Api.Services;

public interface IFlowOrchestrator
{
    Task<FlowRunResponse> RunAsync(FlowRunRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<ExperimentSummary> GetExperimentSummaries();
}

public class FlowOrchestrator : IFlowOrchestrator
{
    private readonly PlannerAgent _planner;
    private readonly RagAgent _rag;
    private readonly CriticAgent _critic;
    private readonly SafetyAgent _safety;
    private readonly FinalizerAgent _finalizer;
    private readonly IExperimentLogger _logger;

    public FlowOrchestrator(
        PlannerAgent planner,
        RagAgent rag,
        CriticAgent critic,
        SafetyAgent safety,
        FinalizerAgent finalizer,
        IExperimentLogger logger)
    {
        _planner = planner;
        _rag = rag;
        _critic = critic;
        _safety = safety;
        _finalizer = finalizer;
        _logger = logger;
    }

    public async Task<FlowRunResponse> RunAsync(FlowRunRequest request, CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid();
        var steps = new List<AgentExecutionRecord>();

        // 1) Plan
        var baseCtx = new AgentContext(
            TaskType: request.TaskType,
            Instruction: request.Instruction,
            InputContext: request.InputContext,
            Domain: request.Domain,
            Plan: null,
            AggregatedNotes: null,
            IntermediateOutput: null);

        var planRes = await _planner.RunAsync(baseCtx, cancellationToken);
        steps.Add(ToRecord(baseCtx, planRes));

        // 2) Retrieve
        var ragCtx = baseCtx with { Plan = planRes.Output };
        var ragRes = await _rag.RunAsync(ragCtx, cancellationToken);
        steps.Add(ToRecord(ragCtx, ragRes));

        // 3) Draft (first pass using FinalizerAgent; recorded as DraftAgent)
        var draftCtx = ragCtx with { InputContext = ragRes.Output };
        var draftRes = await _finalizer.RunAsync(draftCtx, cancellationToken);

        steps.Add(new AgentExecutionRecord(
            AgentName: "DraftAgent",
            Role: "Draft",
            InputSummary: "Used plan + retrieved context to generate first draft.",
            OutputSummary: Truncate(draftRes.Output, 260),
            GovernancePassed: true,
            GovernanceIssues: Array.Empty<string>()
        ));

        // 4) Critique
        var criticCtx = draftCtx with { IntermediateOutput = draftRes.Output };
        var criticRes = await _critic.RunAsync(criticCtx, cancellationToken);
        steps.Add(ToRecord(criticCtx, criticRes));

        // 5) Safety review
        var safetyCtx = criticCtx with { AggregatedNotes = criticRes.Output };
        var safetyRes = await _safety.RunAsync(safetyCtx, cancellationToken);
        steps.Add(ToRecord(safetyCtx, safetyRes));

        // 6) Final
        var finalCtx = safetyCtx with
        {
            InputContext = ragRes.Output,
            IntermediateOutput = draftRes.Output,
            AggregatedNotes = criticRes.Output + (safetyRes.GovernancePassed ? "" : "\n\nSafety issues: " + string.Join("; ", safetyRes.GovernanceIssues))
        };

        var finalRes = await _finalizer.RunAsync(finalCtx, cancellationToken);
        steps.Add(ToRecord(finalCtx, finalRes));

        var response = new FlowRunResponse(
            RunId: runId,
            TaskType: request.TaskType,
            Instruction: request.Instruction,
            InputContext: request.InputContext,
            Domain: request.Domain,
            Plan: planRes.Output,
            Steps: steps,
            FinalOutput: finalRes.Output
        );

        _logger.Log(response);
        return response;
    }

    public IReadOnlyList<ExperimentSummary> GetExperimentSummaries() => _logger.GetSummaries();

    private static AgentExecutionRecord ToRecord(AgentContext ctx, AgentResult result)
    {
        var inputSummary = $"TaskType={ctx.TaskType}; Domain={ctx.Domain ?? "n/a"}";
        return new AgentExecutionRecord(
            AgentName: result.AgentName,
            Role: result.Role,
            InputSummary: Truncate(inputSummary, 180),
            OutputSummary: Truncate(result.Output, 260),
            GovernancePassed: result.GovernancePassed,
            GovernanceIssues: result.GovernanceIssues
        );
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= max ? value : value[..max] + "...";
    }
}
