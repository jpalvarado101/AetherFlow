using AetherFlow.Api.Models;

namespace AetherFlow.Api.Services;

public interface IExperimentLogger
{
    void Log(FlowRunResponse response);
    IReadOnlyList<ExperimentSummary> GetSummaries();
}

public class ExperimentLogger : IExperimentLogger
{
    private readonly List<ExperimentSummary> _runs = new();

    public void Log(FlowRunResponse response)
    {
        var path = string.Join(" -> ", response.Steps.Select(s => s.AgentName));
        var summary = new ExperimentSummary(
            RunId: response.RunId,
            Timestamp: DateTimeOffset.UtcNow,
            TaskType: response.TaskType,
            AgentPath: path,
            GovernancePassed: response.Steps.All(s => s.GovernancePassed),
            StepCount: response.Steps.Count
        );
        _runs.Add(summary);
    }

    public IReadOnlyList<ExperimentSummary> GetSummaries() => _runs.ToList();
}
