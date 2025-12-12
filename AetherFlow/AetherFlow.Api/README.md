# AetherFlow – Multi-Agent AI Prototyping Sandbox (Backend)

AetherFlow is a **C#/.NET 8** backend for experimenting with **AI-native, multi-agent workflows**:

- **Planner → RAG → Draft → Critic → Safety → Finalizer**
- Retrieval-augmented generation (RAG) over a small in-memory “knowledge store”
- Simple governance checks + experiment logging
- OpenAI-compatible REST integration (Chat Completions + Embeddings)

This is a **prototype** intentionally designed to make experimentation easy:
swap prompts/models, replace the vector store, add/replace agents, log runs, and compare behavior.

---

## Tech stack

- .NET 8 Web API (Minimal APIs)
- OpenAI-compatible REST API (Chat + Embeddings)
- In-memory vector search (RAG)
- Agent-style components (Planner, Retriever, Critic, Safety, Finalizer)
- Experiment summaries endpoint

---

## Running locally

### 1) Requirements

- .NET 8 SDK
- An OpenAI-compatible API key (OpenAI or Azure OpenAI with an OpenAI-style endpoint)
- Internet access (for embeddings + chat calls)

### 2) Configure settings

Set the API key via environment variable (recommended):

**macOS / Linux**
```bash
export OpenAI__ApiKey="sk-..."
```

**Windows PowerShell**
```powershell
$env:OpenAI__ApiKey="sk-..."
```

Optional overrides:
- `OpenAI__BaseUrl`
- `OpenAI__ChatModel`
- `OpenAI__EmbeddingModel`

### 3) Run

```bash
dotnet run
```

Swagger UI:
- `https://localhost:<port>/swagger`

---

## Endpoints

### Health
```http
GET /health
```

### Run a multi-agent flow
```http
POST /api/flows/run
Content-Type: application/json
```

Example body:
```json
{
  "taskType": "api-design",
  "instruction": "Propose a REST API for a task tracking service with key endpoints and error handling.",
  "inputContext": null,
  "domain": "backend engineering"
}
```

Response includes:
- `runId` – experiment identifier
- `plan` – PlannerAgent output
- `steps` – per-agent summaries and governance status
- `finalOutput` – FinalizerAgent consolidated answer

### List experiment summaries
```http
GET /api/experiments
```

Returns a list of runs with:
- run id
- timestamp
- task type
- agent path
- governance status
- step count

---

## Architecture (high level)

```
Client -> POST /api/flows/run
  -> PlannerAgent (chat)
  -> RagAgent (embeddings + similarity search)
  -> Draft (chat)
  -> CriticAgent (chat)
  -> SafetyAgent (chat, returns JSON)
  -> FinalizerAgent (chat)
  -> ExperimentLogger (stores summary)
```

---

## Notes

- The current RAG store is intentionally small and in-memory for simplicity.
  Replace `InMemoryKnowledgeStore` with Azure Cognitive Search / pgvector / Qdrant for production.
- You can add additional agents easily by implementing `IAgent` and extending `FlowOrchestrator`.
