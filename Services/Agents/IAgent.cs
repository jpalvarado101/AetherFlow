using AetherFlow.Api.Models;

namespace AetherFlow.Api.Services.Agents;

public interface IAgent
{
    string Name { get; }
    string Role { get; }

    Task<AgentResult> RunAsync(AgentContext context, CancellationToken cancellationToken = default);
}
