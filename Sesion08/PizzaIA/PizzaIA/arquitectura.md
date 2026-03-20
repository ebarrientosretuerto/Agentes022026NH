# Arquitectura del Proyecto - Mi Pizza AI Assistant

## Archivos y su finalidad

```
┌─────────────────────────────────────────────────────────────────┐
│                        CONFIGURACIÓN                            │
│                                                                 │
│  appsettings.json                                               │
│  ├── AzureOpenAI:Endpoint                                       │
│  ├── AzureOpenAI:DeploymentName                                 │
│  └── AzureOpenAI:ApiKey                                         │
└───────────────────────────┬─────────────────────────────────────┘
                            │ IConfiguration
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                     ENTRY POINT                                 │
│                                                                 │
│  Program.cs                                                     │
│  ├── Crea OpenAICliente (Temperature, TopP, TopK, StreamDelay)  │
│  ├── Llama ConnectMcpAsync()                                    │
│  ├── Registra AddChatClient(openAICliente)                      │
│  ├── Registra AddAIAgent("Mi Pizza", prompt, openAICliente)     │
│  └── Mapea endpoints /v1/responses /v1/conversations /devui     │
└──────────┬──────────────────────────────────────────────────────┘
           │ new OpenAICliente()
           ▼
┌─────────────────────────────────────────────────────────────────┐
│                     CHAT CLIENT                                 │
│                                                                 │
│  OpenAICliente.cs  (implements IChatClient)                     │
│  ├── ConnectMcpAsync()  ──────────────────────────────────────┐ │
│  │   ├── Llama AzureClientFactory.Create()                    │ │
│  │   ├── Conecta al servidor MCP remoto (HttpClientTransport) │ │
│  │   └── Carga tools MCP + PizzaDbTools.ObtenerEsquema        │ │
│  │                                                            │ │
│  ├── BuildOptions()                                           │ │
│  │   └── Aplica Temperature/TopP/TopK/Penalties/Seed/Stop     │ │
│  │                                                            │ │
│  ├── GetResponseAsync()                                       │ │
│  │   └── Llama _innerClient.GetResponseAsync()                │ │
│  │                                                            │ │
│  └── GetStreamingResponseAsync()                              │ │
│      ├── Llama _innerClient.GetStreamingResponseAsync()       │ │
│      ├── Aplica StreamDelay entre chunks                      │ │
│      └── Acumula y loguea UsageContent (tokens)               │ │
└──────────┬──────────────────────────────┬─────────────────────┘ │
           │ Create()                     │ tools                  │
           ▼                              ▼                        │
┌──────────────────────┐   ┌─────────────────────────────────┐    │
│  AzureClientFactory  │   │  PizzaDbTools.cs                │    │
│  .cs                 │   │  ├── _schema (lee pizza-db.md)  │◄───┘
│                      │   │  └── ObtenerEsquema()           │
│  Lee appsettings.json│   │      └── retorna el .md completo│
│  Crea                │   └─────────────────────────────────┘
│  AzureOpenAIClient   │                   ▲
│  .GetChatClient()    │                   │ File.ReadAllText()
│  .AsIChatClient()    │   ┌───────────────┴─────────────────┐
│                      │   │  pizza-db.md                    │
│  retorna IChatClient │   │  Diccionario de datos de        │
└──────────────────────┘   │  PizzaStore (tablas, campos,    │
           │               │  tipos, relaciones)             │
           │               └─────────────────────────────────┘
           ▼
┌─────────────────────────────────────────────────────────────────┐
│                     AZURE OPENAI                                │
│                                                                 │
│  gpt-4o-mini deployment                                         │
│  ├── Recibe mensajes + ChatOptions (Temperature, TopP...)       │
│  ├── Recibe tools disponibles                                   │
│  ├── Decide si llamar a ObtenerEsquema                          │
│  └── Genera respuesta SQL en streaming                          │
└─────────────────────────────────────────────────────────────────┘

           ▲  HTTP SSE stream
           │
┌─────────────────────────────────────────────────────────────────┐
│                     DEV UI / CLIENTE                            │
│                                                                 │
│  /devui  →  DevUI (Microsoft.Agents.AI.DevUI)                   │
│  /v1/conversations  →  conversaciones con historial             │
│  /v1/responses      →  respuesta única sin historial            │
└─────────────────────────────────────────────────────────────────┘
```

---

## Flujo de una petición

```
Usuario (DevUI)
    │ "dame los pedidos pendientes"
    ▼
AddAIAgent (prompt de Mi Pizza)
    │ inyecta system prompt + herramientas
    ▼
OpenAICliente.GetStreamingResponseAsync()
    │ BuildOptions() → Temperature, TopP, TopK, tools
    ▼
Azure OpenAI (1ra llamada)
    │ decide llamar ObtenerEsquema
    ▼
PizzaDbTools.ObtenerEsquema()
    │ lee pizza-db.md → retorna esquema
    ▼
Azure OpenAI (2da llamada)
    │ genera SELECT con JOINs correctos
    ▼
Stream de chunks con delay
    │ LogUsage (tokens acumulados)
    ▼
Usuario ve el SQL en el DevUI
```

---

## Resumen de archivos

| Archivo | Tipo | Finalidad |
|---------|------|-----------|
| `appsettings.json` | Configuración | Credenciales y endpoint de Azure OpenAI |
| `Program.cs` | Entry point | Configura DI, registra agente y mapea endpoints |
| `OpenAICliente.cs` | IChatClient | Gestiona streaming, tools, parámetros y token usage |
| `AzureClientFactory.cs` | Factory | Crea el IChatClient de Azure OpenAI |
| `PizzaDbTools.cs` | Tool | Expone el esquema de BD al agente vía función |
| `pizza-db.md` | Datos | Diccionario de datos de PizzaStore |
| `pizza-db.sql` | Script | Script SQL de creación de tablas |
