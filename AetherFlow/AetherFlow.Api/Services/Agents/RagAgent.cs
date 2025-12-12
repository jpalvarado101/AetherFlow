using AetherFlow.Api.Models;

namespace AetherFlow.Api.Services.Agents;

public class RagAgent : IAgent
{
    public string Name => "RagAgent";
    public string Role => "Retriever";

    private readonly IInMemoryKnowledgeStore _knowledgeStore;

    public RagAgent(IInMemoryKnowledgeStore knowledgeStore)
    {
        _knowledgeStore = knowledgeStore;
    }

    public async Task<AgentResult> RunAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var query = $"{context.Instruction} {context.Domain}".Trim();
        var docs = await _knowledgeStore.SearchAsync(query, cancellationToken);

        var combined = string.Join("\n\n---\n\n", docs);

        return new AgentResult(
            AgentName: Name,
            Role: Role,
            Output: combined,
            GovernancePassed: true,
            GovernanceIssues: Array.Empty<string>()
        );
    }
}
