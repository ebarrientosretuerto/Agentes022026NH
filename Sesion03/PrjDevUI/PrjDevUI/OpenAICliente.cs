using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DemoApp;

namespace DemoAPP;

public class OpenAICliente : IChatClient
{
    private ChatClientAgent? _agent;
    private readonly List<ChatMessage> _history = new();

    private const string McpServerUrl = "https://ebarrientosr1979.app.n8n.cloud/mcp/d5ae25fc-482f-4ef9-80ee-983ac1378e6a";


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

    public async Task ConnectMcpAsync(IConfiguration configuration)
    {
        //Cargar las herramientas que se encuentren como Tools
        var innerClient = AzureClientFactory.Create(configuration);

        var agentTools = new List<AITool>();
    /*    {
            AIFunctionFactory.Create(new AgentTools().GetCurrentDateTime)
        };
     */   
        //Conectandonos a nuestro Servido MCP Remoto
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(McpServerUrl) });

        var mcpClient = await McpClient.CreateAsync(transport,
            cancellationToken: CancellationToken.None);

        var mcpTools = await mcpClient
                .ListToolsAsync(cancellationToken: CancellationToken.None);
        foreach(var tool in mcpTools)
            agentTools.Add(tool);
        
        _agent = new ChatClientAgent(
            chatClient: innerClient,
            instructions: "Eres un asistente amable que usa las herramientas cuando sea necesario, incluyendo la calculadora para operaciones matemáticas.",
            name: "AI-Assistant",
            tools: agentTools
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