using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using System.Runtime.CompilerServices;
using PizzaIA;

namespace PizzaIA;

public class OpenAICliente : IChatClient
{
    private IChatClient? _innerClient;
    private List<AITool> _tools = [];
    private const string McpServerUrl = "https://ebarrientosr1979.app.n8n.cloud/mcp/d5ae25fc-482f-4ef9-80ee-983ac1378e6a";

    public float? Temperature { get; set; } = null;
    public float? TopP { get; set; } = null;    
    public float? TopK { get; set; } = null;
    //Variable para determinar el numero maximo de token de respuesta
    public int? MaxOutputTokens { get; set; } = null;
    //Penaliza tokens que ya aparecieron frecuentemente
    public float? FrequencyPenalty { get; set; } = null;
    //Penaliza tokens que ya aparecieron al menos una vez
    public float? PresencePenalty { get; set; } = null;
    public long? Seed { get; set; } = null;
    public IList<string>? StopSequences { get; set; } = null;
    public int StreamDelay { get; set; } = 30;

    public ChatClientMetadata Metadata => new("OpenAICliente");
    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => null;


    public async Task ConnectMcpAsync(IConfiguration configuration)
    {
        
        _innerClient = AzureClientFactory.Create(configuration);
        
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(McpServerUrl) }
            );

        
        var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: CancellationToken.None);
        _tools = (await mcpClient.ListToolsAsync(cancellationToken: CancellationToken.None))
            .Cast<AITool>().ToList();
        
        //Agrega herramienta local del diccionario de datos de Mi Pizza
        _tools.Add(AIFunctionFactory.Create(new PizzaDbTools().ObtenerEsquema));

        //Agrega herramienta de consulta a la base de datos PorstgreSQL
        var queryTool = new PizzaDbQueryTool(configuration);
        _tools.Add(AIFunctionFactory.Create(queryTool.EjecutarConsulta));
        
    }

    private ChatOptions BuildOptions(ChatOptions? incoming = null)
    {
        var opts = incoming ?? new ChatOptions();
        if (Temperature.HasValue) opts.Temperature = Temperature.Value;
        if (TopP.HasValue) opts.TopP = TopP.Value;        
        if (MaxOutputTokens.HasValue) opts.MaxOutputTokens = MaxOutputTokens.Value;
        if (FrequencyPenalty.HasValue) opts.FrequencyPenalty = FrequencyPenalty.Value;
        if (PresencePenalty.HasValue) opts.PresencePenalty = PresencePenalty.Value;
        if (Seed.HasValue) opts.Seed = Seed.Value;
        if (StopSequences != null) opts.StopSequences = StopSequences;

        if (TopK.HasValue)
        {
            opts.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            opts.AdditionalProperties["top_k"] = TopK.Value;
        }

        opts.Tools = [.. opts.Tools ?? [], .. _tools];
        return opts;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
        )
    {
        if (_innerClient == null) throw new InvalidOperationException("OpenAICliente no inicializado");
        var opts = BuildOptions(options);
        return await _innerClient.GetResponseAsync(messages, opts, cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        if (_innerClient == null) throw new InvalidOperationException("OpenIAClient NO Inicializado");

        var opts = BuildOptions(options);

        long totalInput = 0, totalOutput = 0, totalReasoning = 0, totalCached = 0;
        
        await foreach(var update in _innerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            var usage = update.Contents.OfType<UsageContent>().FirstOrDefault();
            if(usage?.Details != null)
            {
                totalInput += usage.Details.InputTokenCount ?? 0;
                totalOutput += usage.Details.OutputTokenCount ?? 0;
                totalReasoning += usage.Details.ReasoningTokenCount ?? 0;
                totalCached += usage.Details.CachedInputTokenCount ?? 0;
            }

            await Task.Delay(StreamDelay, cancellationToken);
            yield return update;
        }

        LogUsage(totalInput, totalOutput, totalReasoning, totalCached);
    }

    private static void LogUsage(long input, long output, long reasoning, long cached)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=====Total de Tokens Usados ===========");
        Console.WriteLine($"   Input Tokens     :   {input}");
        Console.WriteLine($"   Output Tokens    :   {output}");
        Console.WriteLine($"   Total Tokens     :   {input + output}");
        Console.WriteLine($"   Reasoning Tokens :   {reasoning}");
        Console.WriteLine($"   Cached input     :   {cached}");
        Console.WriteLine("=======================================");
        Console.ResetColor();
    }
}
