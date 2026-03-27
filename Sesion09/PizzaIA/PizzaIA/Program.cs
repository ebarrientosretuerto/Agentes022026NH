using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using PizzaIA;
using PizzaIA.RAG;

var builder = WebApplication.CreateBuilder(args);

var openAICliente = new OpenAICliente
{
    Temperature = 0.1f, //Creatividad Moderada
    TopP = 0.9f,        //Probabilidad de respuesta
    TopK = 10,          //Considerar los 50 tokens más probables
    StreamDelay = 20    //50ms entre chunks para efecto de escritura
};

await openAICliente.ConnectMcpAsync(builder.Configuration);

//Registrar los servicios RAG
var embeddingService = new EmbeddingService(builder.Configuration);
var vectorStore = new VectorStore(builder.Configuration);
var ragTool = new RagSearchTool(embeddingService, vectorStore);
var ingestionService = new PdfIngestionService(embeddingService, vectorStore);

openAICliente.AgregarTool(AIFunctionFactory.Create(ragTool.BuscarEnDocumentos));

builder.Services.AddChatClient(openAICliente)
    .UseOpenTelemetry(configure: o => o.EnableSensitiveData = true);

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

builder.Services.AddDevUI();
/*
builder.Services.AddAIAgent("Mi Pizza", """
    Eres el asistente técnico de Mi Pizza, especializado EXCLUSIVAMENTE en generar scripts y queries SQL.

    REGLAS ESTRICTAS:
    - NUNCA respondas con "lo siento", "no puedo" o "no tengo acceso", SIEMPRE genera el SQL.    
    - SIEMPRE llama a la herramienta ObtenerEsquema antes de responder cualquier pregunta.
    - Toda respuesta debe contener un script SQL (CREATE, SELECT, INSERT, UPDATE o DELETE).
    - Usa SIEMPRE el prefijo schema pizza, en el nombre de la tabla.
    - Si el usuario pregunta por pedidos, clientes, productos, pagos, ingredientes o entregas: genera el SELECT con JOINs.
    - Si el usuario pide crear la base de datos: genera el script CREATE TABLE completo.
    - Responde siempre en español con el SQL dentro de un bloque de codigo sql.
    - No des explicaciones largas, ve directo al SQL.
    """, openAICliente);
*/
builder.Services.AddAIAgent("Mi Pizza", """ 
    Eres el asistente exclusivo de la pizzería "Mi Pizza". Tu función es responder preguntas sobre el negocio
    usando la base de datos PostgreSQL y los documentos internos.
 
    REGLAS:
    1. SIEMPRE llama a la herramienta ObtenerEsquema antes de generar cualquier SQL.
    2. Usa SIEMPRE el prefijo schema pizza. en los nombres de tabla.
    3. Cuando el usuario haga una pregunta sobre datos del negocio, genera el SQL y luego llama a
       EjecutarConsulta para obtener los resultados reales de la base de datos.
    4. Si la pregunta es sobre políticas, procedimientos, menú detallado, recetas u otra información
       documental, usa BuscarEnDocumentos para buscar en los PDFs indexados.
    5. Presenta los resultados de forma clara y legible al usuario, no solo el SQL.
    6. Si el usuario pide explícitamente solo el SQL, muéstralo sin ejecutar.
    7. Si el usuario pregunta algo que NO se puede resolver con la base de datos ni los documentos de Mi Pizza
       (temas generales, otros negocios, opiniones, etc.), responde exactamente:
       "Lo siento, solo puedo responder preguntas relacionadas con Mi Pizza 🍕"
    8. Si el usuario saluda, responde brevemente y pregunta en qué puede ayudarle sobre Mi Pizza.
    9. Responde siempre en español.
    """, openAICliente);
/*6.Si el usuario pregunta algo que NO se puede resolver con la base de datos de Mi Pizza
        (temas generales, otros negocios, opiniones, etc.), responde exactamente:
        "Lo siento, solo puedo responder preguntas relacionadas con Mi Pizza - Agente AI"*/
var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();
app.MapGet("/", () => "Foundry DevUI esta en ejecucín /devui para ver el dashboard");

app.MapPost("/api/rag/ingestar", async (HttpRequest request) =>
{
    var ruta = request.Query["ruta"].ToString();
    if (string.IsNullOrEmpty(ruta))
        return Results.BadRequest("Parámetro 'ruta' requerido. Puede ser un archivo PDF o una carpeta.");

    if (Directory.Exists(ruta))
    {
        var resultados = await ingestionService.ProcesarCarpetaAsync(ruta);
        return Results.Ok(resultados);
    }

    if (File.Exists(ruta) && ruta.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
    {
        var resultado = await ingestionService.ProcesarPdfAsync(ruta);
        return Results.Ok(resultado);
    }

    return Results.BadRequest("La ruta no es un archivo PDF válido ni una carpeta existente.");
});


app.Run();
