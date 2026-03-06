using AutoGen.Core;
using AutoGen.OpenAI;
using Microsoft.Extensions.Configuration;
using Azure.AI.OpenAI;
using AutoGen.OpenAI.Extension;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ModelContextProtocol.Protocol;


namespace DemoAPP;

public class OpenAICliente:IChatService
{
    private ChatClient? _chatClient;
    private McpClient? _mcpClient;
    private readonly List<ChatMessage> _history = new();
    private readonly List<ChatTool> _chatTools = new();
    private readonly Dictionary<string, McpClientTool> _mcpTools = new();

    /// <summary>
    /// Configurar el cleinte de Azure Open AI y levantar el servidor MCP como subproceso
    /// </summary>   

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

        _chatClient = azureOpenAIClient.GetChatClient(deploymentName);

        _history.Add(new SystemChatMessage(
            "Eres un asistente de IA servicial. Tienes acceso a herramientas. " +
            "Cuando el usuario pregunte por la hora, usa la herramienta GetCurrentTime del servidor MCP"
        ));

        InitializeMcpAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeMcpAsync()
    {
        //Ruta del ejecutable actual para lanzar el servidor MCP como subproceso
        var exePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("No se pudo determinar la ruta del ejecutable");

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = exePath,
            Arguments = ["--with-mcp"] 
        });

        _mcpClient = await McpClient.CreateAsync(transport);

        //Listar las tools del servidor MCP
        var tools = await _mcpClient.ListToolsAsync();
        foreach (var tool in tools)
        {
            _mcpTools[tool.Name] = tool;
            BinaryData? parameters = tool.JsonSchema.ValueKind != JsonValueKind.Undefined
                ? BinaryData.FromString(tool.JsonSchema.GetRawText()) : null;

            _chatTools.Add(ChatTool.CreateFunctionTool(
                functionName: tool.Name,
                functionDescription: tool.Description ?? "",
                functionParameters: parameters
                ));
        }

        Console.WriteLine($"[MCP] {_mcpTools.Count} herramienta(s) registrada(s): {string.Join(", ", _mcpTools.Keys)}");
    }

    public async Task<string> SendMessageAsync(string message)
    {
        if (_chatClient == null)
        {
            throw new InvalidOperationException("OpenAICliente debe estar inicializado.");
        }

        _history.Add(new UserChatMessage(message));

        var options = new ChatCompletionOptions();
        foreach(var tool in _chatTools)
           options.Tools.Add(tool);

        //Bucle de tool-calling: el modelo puede pedir invocar herramientas
        while (true)
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(_history, options);

            if(completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                _history.Add(new AssistantChatMessage(completion));

                foreach(var toolCall in completion.ToolCalls)
                {
                    string result;
                    if (_mcpTools.ContainsKey(toolCall.FunctionName))
                    {
                        var args = string.IsNullOrEmpty(toolCall.FunctionArguments?.ToString())
                            ? new Dictionary<string, object?>()
                            : JsonSerializer.Deserialize<Dictionary<string, object?>>(
                                toolCall.FunctionArguments.ToString()) ?? new();

                        var mcpResult = await _mcpClient!.CallToolAsync(toolCall.FunctionName, args);

                        //Extraer el texto como resultado en MCP
                        var texts = mcpResult.Content
                            .Select(c => (TextContentBlock)c)
                            .Select(t => t.Text);
                        result = string.Join("", texts);
                    }
                    else
                    {
                        result = $"Herramienta '{toolCall.FunctionName}' no encontrada.";
                    }

                    _history.Add(new ToolChatMessage(toolCall.Id, result));
                }
                continue;
            }

            string content = completion.Content[0].Text;
            _history.Add(new AssistantChatMessage(content));
            return content;
        }
    }
}