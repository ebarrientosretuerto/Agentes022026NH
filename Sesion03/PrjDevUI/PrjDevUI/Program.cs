using DemoAPP;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

var openAICliente = new OpenAICliente();

openAICliente.Initialize(builder.Configuration);
//await openAICliente.ConnectMcpAsync(builder.Configuration);

builder.Services.AddChatClient(openAICliente)
    .UseOpenTelemetry(configure: o => o.EnableSensitiveData = true);

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();
builder.Services.AddDevUI();

builder.Services.AddAIAgent("Mi Asistente AI", "Eres un asistente generico");

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

if(app.Environment.IsDevelopment())
{
    app.MapDevUI();
}

app.MapGet("/", () => "Foundry DevUI esta en ejecucion /devui para ver el dashboard");

app.Run();