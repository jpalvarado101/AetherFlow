using AetherFlow.Api.Config;
using AetherFlow.Api.Models;
using AetherFlow.Api.Services;
using AetherFlow.Api.Services.Agents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpenAIOptions>(
    builder.Configuration.GetSection(OpenAIOptions.SectionName));

builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();

builder.Services.AddSingleton<IInMemoryKnowledgeStore, InMemoryKnowledgeStore>();
builder.Services.AddSingleton<IExperimentLogger, ExperimentLogger>();

builder.Services.AddSingleton<PlannerAgent>();
builder.Services.AddSingleton<RagAgent>();
builder.Services.AddSingleton<CriticAgent>();
builder.Services.AddSingleton<SafetyAgent>();
builder.Services.AddSingleton<FinalizerAgent>();

builder.Services.AddSingleton<IFlowOrchestrator, FlowOrchestrator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/flows/run", async (
    FlowRunRequest request,
    IFlowOrchestrator orchestrator,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Instruction))
    {
        return Results.BadRequest(new { error = "Instruction is required." });
    }

    try
    {
        var result = await orchestrator.RunAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[flows/run] Error: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/experiments", (IFlowOrchestrator orchestrator) =>
{
    var summaries = orchestrator.GetExperimentSummaries();
    return Results.Ok(summaries);
});

app.Run();
