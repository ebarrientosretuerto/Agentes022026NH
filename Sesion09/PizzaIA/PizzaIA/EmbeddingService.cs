using Azure.AI.OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;


namespace PizzaIA.RAG;

public class EmbeddingService
{
    private readonly EmbeddingClient _client;

    public EmbeddingService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("Falta Azure OpenAI:Endpoint");
        var apiKey = configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Falta Azure OpenAI:ApiKey");
        var model = configuration["AzureOpenAI:EmbeddingModel"]
            ?? "text-embedding-3-small";

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        _client = azureClient.GetEmbeddingClient(model);
    }

    public async Task<float[]>GenerarEmbeddingAsync(string texto)
    {
        var result = await _client.GenerateEmbeddingAsync(texto);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<List<float[]>> GenerarEmbeddingsBatchAsync(IList<string> textos)
    {
        var result = await _client.GenerateEmbeddingsAsync(textos);
        return result.Value.Select(e => e.ToFloats().ToArray()).ToList();

    }
}