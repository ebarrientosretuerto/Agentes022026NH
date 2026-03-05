using Azure.Core.Extensions;
using DemoApp;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DemoAPP;

public class OpenAICliente : IChatClient
{
    private ChatClientAgent? _agent;
    private readonly List<ChatMessage> _history = new();

    public ChatClientMetadata Metadata => new("OpenAICliente");
    public void Dispose() => _agent = null;
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Initialize(IConfiguration configuration)
    {
        var innerClient = AzureClientFactory.Create(configuration);
        var agentTools = new AgentTools();
        var tools = new List<AITool>
        {
            //AIFunctionFactory.Create(agentTools.GetCurrentDateTime)
        };

        _agent = new ChatClientAgent(
            chatClient: innerClient,
            instructions: "Eres un asistente amable. Usa las herramientas cuando sea necesario",
            name: "AI-Assistant",
            tools:tools
            );
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_agent == null) throw new InvalidOperationException("OpenAICliente no inicializado");

        var response = await _agent.RunAsync(messages.ToList(), cancellationToken: cancellationToken);

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, response.Text ?? "Sin Respuesta"));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate
        {
            Contents = response.Messages.LastOrDefault()?.Contents ?? []
        };
    }
}