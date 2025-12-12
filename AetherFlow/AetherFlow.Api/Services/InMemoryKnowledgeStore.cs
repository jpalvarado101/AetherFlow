namespace AetherFlow.Api.Services;

public interface IInMemoryKnowledgeStore
{
    Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal in-memory vector store to demonstrate RAG. 
/// - Embeds a small set of "knowledge snippets" once (cached).
/// - Embeds each query at runtime.
/// - Returns top-k snippets via cosine similarity.
/// 
/// Swap this for Azure Cognitive Search, pgvector, Qdrant, etc. for production.
/// </summary>
public class InMemoryKnowledgeStore : IInMemoryKnowledgeStore
{
    private readonly IOpenAIService _openAI;
    private bool _initialized;

    private readonly List<(string Text, IReadOnlyList<double> Embedding)> _items = new();

    public InMemoryKnowledgeStore(IOpenAIService openAI)
    {
        _openAI = openAI;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        _initialized = true;

        var docs = new[]
        {
            "Best practices for designing REST APIs: resource-oriented endpoints, consistent naming, pagination, versioning, and error contracts.",
            "Guidelines for building maintainable services: separation of concerns, clear contracts, dependency injection, and observability.",
            "Testing strategies: unit tests for business logic, integration tests for boundaries, end-to-end tests for user journeys, plus mocking strategies.",
            "Retrieval-Augmented Generation (RAG): retrieve relevant context and ground the model response, reducing hallucinations and improving specificity.",
            "AI agent workflows: plan → retrieve → draft → critique → safety check → finalize; record artifacts for evaluation and iteration.",
            "Operational patterns: retries with backoff, timeouts, idempotency keys, and structured logging for production systems."
        };

        foreach (var d in docs)
        {
            var emb = await _openAI.EmbedAsync(d, cancellationToken);
            _items.Add((d, emb));
        }
    }

    public async Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var queryEmb = await _openAI.EmbedAsync(query, cancellationToken);

        var scored = _items
            .Select(i => (i.Text, Score: CosineSimilarity(i.Embedding, queryEmb)))
            .OrderByDescending(x => x.Score)
            .Take(3)
            .Select(x => x.Text)
            .ToList();

        return scored;
    }

    private static double CosineSimilarity(IReadOnlyList<double> a, IReadOnlyList<double> b)
    {
        var len = Math.Min(a.Count, b.Count);
        if (len == 0) return 0;

        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        if (na == 0 || nb == 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
