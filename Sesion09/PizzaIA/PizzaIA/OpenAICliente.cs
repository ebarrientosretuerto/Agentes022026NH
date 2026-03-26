using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using System.Runtime.CompilerServices;
using PizzaIA;
using Microsoft.Agents.AI;
using PizzaIA.Guardrails;
using Microsoft.VisualBasic;


namespace PizzaIA;

public class OpenAICliente : IChatClient
{
    private IChatClient? _innerClient;
    private readonly RateLimit _rateLimiter = new(maxRequest:20, window:TimeSpan.FromMinutes(1));
    private List<AITool> _tools = [];
    private const string McpServerUrl = "https://ebarrientosr1979.app.n8n.cloud/mcp/d5ae25fc-482f-4ef9-80ee-983ac1378e6a";
    private readonly ChatMemoryStore _memory = new();

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
        
        //var transport = new HttpClientTransport(
        //    new HttpClientTransportOptions { Endpoint = new Uri(McpServerUrl) }
        //    );

        
        //var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: CancellationToken.None);
        //_tools = (await mcpClient.ListToolsAsync(cancellationToken: CancellationToken.None))
        //    .Cast<AITool>().ToList();
        
        //Agrega herramienta local del diccionario de datos de Mi Pizza
        _tools.Add(AIFunctionFactory.Create(new PizzaDbTools().ObtenerEsquema));

        //Agrega herramienta de consulta a la base de datos PorstgreSQL
        var queryTool = new PizzaDbQueryTool(configuration);
        _tools.Add(AIFunctionFactory.Create(queryTool.EjecutarConsulta));

        //Crear sub-agente formateador Markdown
        AIAgent htmlAgent = AzureClientFactory.Create(configuration)
            .AsAIAgent(
                instructions: HtmlFormatterAgent.Instructions,
                name: HtmlFormatterAgent.Name,
                description: "Agente que formatea datos en Markdown limpio y legible. Enviale datosy devuelves Markdown"
            );
        _tools.Add(htmlAgent.AsAIFunction());
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
        var sessionId = ExtractSessionID(opts);

        Console.Write("RateLimit");
        //Aca tenemos el prompt de entrada y lo validamos con los Guardrails
        //Guardrail: RateLimit
        var (isAllowed, rateLimitMsg) = _rateLimiter.TryAcquire(sessionId);
        if (!isAllowed)
            return new ChatResponse([new ChatMessage(ChatRole.Assistant, rateLimitMsg)]);

        Console.Write("ValidateUserInput");
        var guardResult = ValidateUserInput(messages);
        if (guardResult != null)
            return new ChatResponse([new ChatMessage(ChatRole.Assistant, guardResult)]);
        Console.Write("Continua...");

        var messageWithHistory = BuildMessageWithHistory(sessionId, messages);

        var response = await _innerClient.GetResponseAsync(messages, opts, cancellationToken);

        //Guardrail: Sanitizar output del modelo
        SanitizeResponseMessage(response.Messages);

        SaveTurnToMemory(sessionId, messages, response.Messages);
        return response;
    }

    private static void SanitizeResponseMessage(IList<ChatMessage> messages)
    {
        for (int i = 0; i<messages.Count; i++)
        {
            if (messages[i].Role == ChatRole.Assistant && messages[i].Text != null)
            {
                var sanitized = OutputGuardrail.Sanitize(messages[i].Text);
                if(sanitized != messages[i].Text)
                {
                    messages[i] = new ChatMessage(ChatRole.Assistant, sanitized);
                }
            }
        }
    }

    private string? ValidateUserInput(IEnumerable<ChatMessage> messages)
    {
        Console.Write("Ejecutando el metodo Guardrail ValidateUserInput");
        foreach(var msg in messages.Where(m => m.Role == ChatRole.User))
        {
            var text = msg.Text;
            var (isValid, errorMessage) = InputGuardrail.Validate(text);
            if (!isValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[InputGuardrail] Mensaje bloqueado: {{errorMessage}}");
                Console.WriteLine($"[InputGuardrail] Input: {text?[..Math.Min(100, text?.Length ?? 0)]}");
                Console.ResetColor();
                return errorMessage;
            }
        }
        return null;
    }

    private List<ChatMessage> BuildMessageWithHistory(string sessionId, 
        IEnumerable<ChatMessage> currentMessages)
    {
        var history = _memory.GetHistory(sessionId);
        var current = currentMessages.ToList();

        var systemMessages = current.Where(m => m.Role == ChatRole.System).ToList();
        var nonSystemMessages = current.Where(m => m.Role != ChatRole.System).ToList();

        var combined = new List<ChatMessage>();

        combined.AddRange(systemMessages);

        lock (history)
        {
            combined.AddRange(history);
        }

        combined.AddRange(nonSystemMessages);

        return combined;
    }

    private void SaveTurnToMemory(string sessionId, IEnumerable<ChatMessage> userMessage, 
        IList<ChatMessage> responseMessage)
    {
        foreach (var msg in userMessage.Where(m => m.Role == ChatRole.User))
            _memory.AddMessage(sessionId, msg);

        foreach (var msg in responseMessage.Where(m => m.Role == ChatRole.Assistant))
            _memory.AddMessage(sessionId, msg);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default)
    {
        if (_innerClient == null) throw new InvalidOperationException("OpenIAClient NO Inicializado");

        var opts = BuildOptions(options);
        var sessionId = ExtractSessionID(opts);
        var messagesWithHistory = BuildMessageWithHistory(sessionId, messages);
        var assistantTextBuilder = new System.Text.StringBuilder();

        long totalInput = 0, totalOutput = 0, totalReasoning = 0, totalCached = 0;

        // --- SECCIÓN CORREGIDA ---
        var (isAllowed, rateLimitMsg) = _rateLimiter.TryAcquire(sessionId);
        if (!isAllowed)
        {
            yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new TextContent(rateLimitMsg)] };
            yield break;
        }

        var guardResult = ValidateUserInput(messages);
        if (guardResult != null)
        {
            yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [new TextContent(guardResult)] };
            yield break;
        }
        // --- FIN SECCIÓN CORREGIDA ---

        // Importante: Usar messagesWithHistory aquí
        await foreach (var update in _innerClient.GetStreamingResponseAsync(messagesWithHistory, opts, cancellationToken))
        {
            // 1. Extraer y acumular el texto para la memoria
            foreach (var content in update.Contents)
            {
                if (content is TextContent textContent)
                {
                    assistantTextBuilder.Append(textContent.Text);
                }
            }

            // 2. Extraer métricas de uso
            var usage = update.Contents.OfType<UsageContent>().FirstOrDefault();
            if (usage?.Details != null)
            {
                totalInput += usage.Details.InputTokenCount ?? 0;
                totalOutput += usage.Details.OutputTokenCount ?? 0;
                totalReasoning += usage.Details.ReasoningTokenCount ?? 0;
                totalCached += usage.Details.CachedInputTokenCount ?? 0;
            }

            await Task.Delay(StreamDelay, cancellationToken);
            yield return update;
        }

        // Guardar el turno en memoria: mensaje del usuario + respuesta completa acumulada
        var userMessages = messages.Where(m => m.Role == ChatRole.User);
        foreach (var msg in userMessages)
            _memory.AddMessage(sessionId, msg);

        var assistantFullText = assistantTextBuilder.ToString();
        if (!string.IsNullOrEmpty(assistantFullText))
            _memory.AddMessage(sessionId, new ChatMessage(ChatRole.Assistant, assistantFullText));

        LogUsage(totalInput, totalOutput, totalReasoning, totalCached);
    }

    private static string ExtractSessionID(ChatOptions opts)
    {
        if (opts.ConversationId is string convId && !string.IsNullOrEmpty(convId))
            return convId;

        if (opts.AdditionalProperties?.TryGetValue("conversationId", out var val) == true
            && val is string sid && !string.IsNullOrEmpty(sid))
            return sid;

        return "default";
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
