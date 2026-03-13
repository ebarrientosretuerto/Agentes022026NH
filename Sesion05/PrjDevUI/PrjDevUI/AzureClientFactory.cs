using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using System.ClientModel;

namespace DemoApp;

public static class AzureClientFactory
{
    public static IChatClient Create(IConfiguration configuration)
    {
        string endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new
            InvalidOperationException("No se encuentra el endpoint");
        string model = configuration["AzureOpenAI:DeploymentName"] ?? throw new
            InvalidOperationException("No se encuentra el modelo");
        string apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new
            InvalidOperationException("No se encuentra el apiKey");
        Console.WriteLine(endpoint);
        Console.WriteLine(model);

        return new AzureOpenAIClient(new Uri(endpoint),
            new ApiKeyCredential(apiKey))
            .GetChatClient(model)
            .AsIChatClient();
    }
}
