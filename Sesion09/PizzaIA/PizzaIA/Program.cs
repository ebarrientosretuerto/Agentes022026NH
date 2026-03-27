using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using PizzaIA;

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
var vectorStore = new VectoStore(builder.Configuration);
var ragTool = new RagSearchTool(embeddingService, vectorStore);
var ingestionService = new PdfIngestionService(embeddingService, vectorStore)


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
    Eres el asistente exclusivo de la pizzeria "Mi Pizza". Tu función es responder preguntas sobre el 
    negocio usando la base de datos PostgresSQL.

    REGLAS:
    1.  SIEMPRE llama a la herramienta ObtenerEsque,a antes de generar cualquier SQL.    
    2.  Usa SIEMPRE el prefijo schema pizza en los nombres de tabla.
    3.  Cuando el usuario haga una pregunta sobre datos del negocio,  genera el SQL y luego llama a
        EjecutarConsulta para obtener los resultados reales de la base de datos.
    4.  SIEMPRE que recibas resultados de EjecucionConsulta, envía esos resultados completos al
        agente "Formateador Markdown" para que los convierta en Markdown. Luego muestra al usuario 
        el Markdown que devuelve el formateador, sin modificarlo.
    5.  Si el usuario pide explicitamente solo el SQL, muestralo sin ejecutar ni formatear.    
    6.  Si el usuario saluda, responde brevemente y pregunta en qué puedo ayudarte sobre Mi Pizza.
    7.  Responde siempre en español. 
    """, openAICliente);
/*6.Si el usuario pregunta algo que NO se puede resolver con la base de datos de Mi Pizza
        (temas generales, otros negocios, opiniones, etc.), responde exactamente:
        "Lo siento, solo puedo responder preguntas relacionadas con Mi Pizza - Agente AI"*/
var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();
app.MapGet("/", () => "Foundry DevUI esta en ejecucín /devui para ver el dashboard");

app.Run();
