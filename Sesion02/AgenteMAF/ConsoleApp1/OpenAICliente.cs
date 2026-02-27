using Microsoft.Agents.AI;
using AutoGen.Core;
using AutoGen.OpenAI;
using Microsoft.Extensions.Configuration;
using Azure.AI.OpenAI;
using AutoGen.OpenAI.Extension;


namespace DemoAPP;

public class OpenAICliente:IChatService
{
    private IAgent? _assistantAgent;

    public void Initialize(IConfiguration configuration)
    {
        string endpoint = configuration["AzureOpenAI:Endpoint"] ?? 
            throw new InvalidOperationException("Endpoint No Encontrado");
        string deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? 
            throw new InvalidOperationException("DeploymentName No Encontrado");
        string apiKey = configuration["AzureOpenAI:ApiKey"] ??
            throw new InvalidOperationException("ApiKey no encontrado");

        var azureOpenAIClient = new AzureOpenAIClient(
            new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey)
            );
        var chatClient = azureOpenAIClient.GetChatClient(deploymentName);

        //Crear el agente de conversacion
        _assistantAgent = new OpenAIChatAgent(
            chatClient: chatClient,
            name: "asistente",
            systemMessage: "Eres un asistente de IA Profesional y Servicial."
            ).RegisterMessageConnector();
    }

    public async Task<string> SendMessageAsync(string message)
    {
        if (_assistantAgent == null)
        {
            throw new InvalidOperationException("OpenAICliente debe estar inicializado.");
        }

        try
        {
            var reply = await _assistantAgent.SendAsync(message);
            return reply.GetContent() ?? "Sin respuesta.";
        }
        catch (Exception ex)
        {
            return $"Error (OpenAI): {ex.Message}";
        }        
    }
}