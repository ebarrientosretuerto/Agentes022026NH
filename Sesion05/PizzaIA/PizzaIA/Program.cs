using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using PizzaIA;

var builder = WebApplication.CreateBuilder(args);

var openAICliente = new OpenAICliente
{
    Temperature = 0.7f, //Creatividad Moderada
    TopP = 0.9f,        //Probabilidad de respuesta
    TopK = 50,          //Considerar los 50 tokens más probables
    StreamDelay = 50    //50ms entre chunks para efecto de escritura
};

await openAICliente.ConnectMcpAsync(builder.Configuration);

builder.Services.AddChatClient(openAICliente)
    .UseOpenTelemetry(configure: o => o.EnableSensitiveData = true);

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

builder.Services.AddDevUI();
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

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();
app.MapGet("/", () => "Foundry DevUI esta en ejecucín /devui para ver el dashboard");

app.Run();
