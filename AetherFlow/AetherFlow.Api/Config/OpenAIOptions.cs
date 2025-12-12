namespace AetherFlow.Api.Config;

public class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ChatModel { get; set; } = "gpt-4.1-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}
