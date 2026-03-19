using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using System.ClientModel;

namespace PizzaIA;

public  static class AzureClientFactory
{
    public static IChatClient Create(IConfiguration configuration){
        string endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new
            InvalidOperationException("No se encontro el endpoint");
        string model = configuration["AzureOpenAI:DeploymentName"] ?? throw new
            InvalidOperationException("No se encontro el modelo");
        string apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new
            InvalidOperationException("No se encontro el apiKey");

        return new AzureOpenAIClient(new Uri(endpoint),
                new ApiKeyCredential(apiKey))
            .GetChatClient(model)
            .AsIChatClient();
    }

}