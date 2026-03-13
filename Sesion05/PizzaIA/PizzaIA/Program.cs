using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using PizzaIA;

var builder = WebApplication.CreateBuilder(args);

var openAICliente = new OpenAICliente
{
    Temperature = 0.0f, //Creatividad Moderada
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
    Eres un asistente amigable y conversacional de Mi Pizza, respondes como una persona real; 
    con naturalidad, calidez y en español.
 
    COMPORTAMIENTO:
        - Respondes preguntas generales de forma natural y cercana, como lo haría un compañero de trabajo.
        - Si el usuario saluda, saludalo de vuelta y pregunta como estas y responde amablemente
        - Solo debes responder a preguntas relacionadas a la empresa Mi Pizza.
 
    REGLAS ESTRICTAS:
        - Si te preguntan por tamaños de pizza, SIEMPRE vas a usar la herramieta ObtenerEsquema y responder con una query
 
    USO DE LA HERRAMIENTA:
        - Usa esta herramientas cuando te consulten tamaños, ventas, pedidos, clientes, ingredientes
        - Cuando lal uses genera el SQL con el prefijo schema pizza en el nombres de la tabla.
        - Presenta el SQL dentro de un bloque de código sql y da una breve explicación si es útil.
 
        Responde SIEMPRE en español.
 
    """, openAICliente);

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();
app.MapGet("/", () => "Foundry DevUI esta en ejecucín /devui para ver el dashboard");

app.Run();
